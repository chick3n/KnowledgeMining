// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.Application.Common.Options;
using KnowledgeMining.Application.Documents.Commands.DeleteDocument;
using KnowledgeMining.Application.Documents.Queries.SearchDocuments;
using KnowledgeMining.Domain.Entities;
using KnowledgeMining.UI.Services.Search.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace KnowledgeMining.Infrastructure.Services.Search
{
    public class SearchService : ISearchService
    {
        private readonly SearchIndexClient _searchIndexClient;
        private readonly ChannelWriter<SearchIndexerJobContext> _jobChannel;

        private readonly Application.Common.Options.SearchOptions _searchOptions;
        private readonly EntityMapOptions _entityMapOptions;

        private readonly ILogger _logger;

        public SearchService(SearchIndexClient searchIndexClient,
                             ChannelWriter<SearchIndexerJobContext> jobChannel,
                             IOptions<Application.Common.Options.SearchOptions> searchOptions,
                             IOptions<EntityMapOptions> entityMapOptions,
                             ILogger<SearchService> logger)
        {
            _searchIndexClient = searchIndexClient;
            _jobChannel = jobChannel;

            _searchOptions = searchOptions.Value;
            _entityMapOptions = entityMapOptions.Value;

            _logger = logger;
        }

        // TODO: Add schema to a cache
        public async Task<Schema> GenerateSearchSchema(string indexName, CancellationToken cancellationToken)
        {
            var response = await _searchIndexClient.GetIndexAsync(indexName, cancellationToken);

            return new Schema(response.Value.Fields);
        }

        private SearchClient GetSearchClient(string indexName)
        {
            return _searchIndexClient.GetSearchClient(indexName);
        }

        public async Task<IEnumerable<string>> Autocomplete(string indexName, string searchText, bool fuzzy, CancellationToken cancellationToken)
        {
            // Execute search based on query string
            AutocompleteOptions options = new()
            {
                Mode = AutocompleteMode.OneTermWithContext,
                UseFuzzyMatching = fuzzy,
                Size = _searchOptions.PageSize
            };

            var response = await GetSearchClient(indexName)
                .AutocompleteAsync(searchText, _searchOptions.SuggesterName, options, cancellationToken);


            return response.Value.Results.Select(r => r.Text).Distinct();
        }

        public async Task<SearchDocumentsResponse> SearchDocuments(SearchDocumentsQuery request, CancellationToken cancellationToken)
        {
            var searchSchema = await GenerateSearchSchema(request.Index.IndexName, cancellationToken);
            var searchOptions = GenerateSearchOptions(request, searchSchema);

            var searchResults = await GetSearchClient(request.Index.IndexName)
                .SearchAsync<DocumentMetadata>(request.SearchText, searchOptions, cancellationToken);

            if (searchResults == null || searchResults?.Value == null)
            {
                return new SearchDocumentsResponse();
            }

            return new SearchDocumentsResponse()
            {
                TotalCount = searchResults.Value.TotalCount ?? 0,
                Documents = searchResults.Value.GetResults().Select(d => d.Document),
                Facets = SummarizeFacets(searchResults.Value.Facets),
                // Not sure if I need to return page in the search result
                TotalPages = CalculateTotalPages(searchResults.Value.TotalCount ?? 0),
                FacetableFields = searchSchema.Facets.Select(f => f.Name), // Not sure if I need to return page in the search result
                SearchId = ParseSearchId(searchResults)
            };
        }

        public async Task<DocumentMetadata> GetDocumentDetails(string indexName, string documentId, CancellationToken cancellationToken)
        {
            var response = await GetSearchClient(indexName)
                .GetDocumentAsync<DocumentMetadata>(documentId, cancellationToken: cancellationToken);

            return response.Value;
        }
        private async Task<SearchResults<SearchDocument>> GetFacets(string indexName, string searchText, IEnumerable<string> facetNames, int maxCount, CancellationToken cancellationToken)
        {
            var facets = new List<string>();

            foreach (var facet in facetNames)
            {
                facets.Add($"{facet}, count:{maxCount}");
            }

            // Execute search based on query string
            var options = new Azure.Search.Documents.SearchOptions()
            {
                SearchMode = SearchMode.Any,
                Size = 10,
                QueryType = SearchQueryType.Full
            };

            foreach (string s in facetNames)
            {
                options.Facets.Add(s);
            }

            return await GetSearchClient(indexName)
                .SearchAsync<SearchDocument>(EscapeSpecialCharacters(searchText), options, cancellationToken);
        }

        private string EscapeSpecialCharacters(string searchText)
        {
            return Regex.Replace(searchText, @"([-+&|!(){}\[\]^""~?:/\\])", @"\$1", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public async Task<EntityMap> GenerateEntityMap(string indexName,
            string? q, 
            IEnumerable<string> facetNames, 
            int maxLevels, 
            int maxNodes,
            CancellationToken cancellationToken)
        {
            var query = "*";

            // If blank search, assume they want to search everything
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = q;
            }

            var facets = new List<string>();

            if (facetNames?.Any() ?? false)
            {
                facets.AddRange(facetNames);
            }
            else
            {
                var schema = await GenerateSearchSchema(indexName, cancellationToken);
                var facetablesFacets = schema.Facets.Where(f => f.IsFacetable).Select(f => f.Name);
                if (facetablesFacets.Any())
                {
                    facets.AddRange(facetablesFacets!);
                }
            }

            var entityMap = new EntityMap();

            // Calculate nodes for N levels 
            int CurrentNodes = 0;
            int originalDistance = 100;

            List<FDGraphEdges> FDEdgeList = new List<FDGraphEdges>();
            // Create a node map that will map a facet to a node - nodemap[0] always equals the q term

            var NodeMap = new Dictionary<string, NodeInfo>();

            NodeMap[query] = new NodeInfo(CurrentNodes, "0")
            {
                Distance = originalDistance,
                Layer = 0
            };

            List<string> currentLevelTerms = new List<string>();

            List<string> NextLevelTerms = new List<string>();
            NextLevelTerms.Add(query);

            // Iterate through the nodes up to MaxLevels deep to build the nodes or when I hit max number of nodes
            for (var CurrentLevel = 0; CurrentLevel < maxLevels && maxNodes > 0; ++CurrentLevel, maxNodes /= 2)
            {
                currentLevelTerms = NextLevelTerms.ToList();
                NextLevelTerms.Clear();
                var levelNodeCount = 0;

                NodeInfo? densestNodeThisLayer = default;
                var density = 0;

                foreach (var k in NodeMap)
                    k.Value.Distance += originalDistance;

                foreach (var t in currentLevelTerms)
                {
                    if (levelNodeCount >= maxNodes)
                        break;

                    int facetsToGrab = 10;

                    if (maxNodes < 10)
                    {
                        facetsToGrab = maxNodes;
                    }

                    var response = await GetFacets(indexName, t, facets, facetsToGrab, cancellationToken);

                    if (response != null)
                    {
                        int facetColor = 0;

                        foreach (var facet in facets)
                        {
                            var facetVals = response.Facets[facet];
                            facetColor++;

                            foreach (var facetResult in facetVals)
                            {
                                var facetValue = facetResult!.Value.ToString() ?? string.Empty;

                                if (!NodeMap.TryGetValue(facetValue, out NodeInfo? nodeInfo))
                                {
                                    // This is a new node
                                    ++levelNodeCount;
                                    NodeMap[facetValue] = new NodeInfo(++CurrentNodes, facetColor.ToString())
                                    {
                                        Distance = originalDistance,
                                        Layer = CurrentLevel + 1
                                    };

                                    if (CurrentLevel < maxLevels)
                                    {
                                        NextLevelTerms.Add(facetValue);
                                    }
                                }

                                // Add this facet to the fd list
                                var newNode = NodeMap[facetValue];
                                var oldNode = NodeMap[t];
                                if (oldNode != newNode)
                                {
                                    oldNode.ChildCount += 1;
                                    if (densestNodeThisLayer == null || oldNode.ChildCount > density)
                                    {
                                        density = oldNode.ChildCount;
                                        densestNodeThisLayer = oldNode;
                                    }

                                    FDEdgeList.Add(new FDGraphEdges
                                    {
                                        Source = oldNode.Index,
                                        Target = newNode.Index,
                                        Distance = newNode.Distance
                                    });
                                }
                            }
                        }
                    }
                }

                if (densestNodeThisLayer != null)
                    densestNodeThisLayer.LayerCornerStone = CurrentLevel;
            }

            foreach (KeyValuePair<string, NodeInfo> entry in NodeMap)
            {
                entityMap.Nodes.Add(new EntityMapNode()
                {
                    Name = entry.Key,
                    Id = entry.Value.Index,
                    Color = entry.Value.ColorId,
                    Layer = entry.Value.Layer,
                    CornerStone = entry.Value.LayerCornerStone
                });
            }

            FDEdgeList.ForEach(e => entityMap.Links.Add(new EntityMapLink()
            {
                Distance = e.Distance,
                Source = e.Source,
                Target = e.Target
            }));

            return entityMap;
        }

        public ValueTask QueueIndexerJob(CancellationToken cancellationToken)
        {
            return _jobChannel.WriteAsync(new SearchIndexerJobContext(), cancellationToken);
        }

        private long CalculateTotalPages(long resultsTotalCount)
        {
            var pageCount = resultsTotalCount / _searchOptions.PageSize;

            if (resultsTotalCount % _searchOptions.PageSize > 0)
            {
                pageCount++;
            }

            return pageCount;
        }

        private string ParseSearchId(Response<SearchResults<DocumentMetadata>> searchResults)
        {
            string? searchId = default;

            if (searchResults.GetRawResponse().Headers.TryGetValues("x-ms-azs-searchid", out IEnumerable<string>? headerValues))
            {
                searchId = headerValues?.FirstOrDefault();
            }
            return searchId ?? string.Empty;
        }

        private IEnumerable<SummarizedFacet> SummarizeFacets(IDictionary<string, IList<FacetResult>> facets)
        {
            return facets
                .Select(f => new SummarizedFacet()
            {
                Name = f.Key,
                Count = f.Value.Count,
                Values = f.Value.Select(v => new Facet()
                {
                    Name = f.Key,
                    Value = ValueFacetResult(v),
                    Count = v.Count ?? 0
                })
            });
        }

        private string ValueFacetResult(FacetResult f)
        {
            try
            {
                return f.AsValueFacetResult<string>().Value;
            }
            catch(InvalidCastException ex)
            {
                _logger.LogError(ex, $"Unabled to cast facet result.");

            }
            return f.Value?.ToString() ?? string.Empty;
        }

        private IEnumerable<string> GenerateFacets(IReadOnlyCollection<SchemaField> facets, IReadOnlyList<FacetFilter> facetFilters)
        {
            var results = new List<string>(); 
            facetFilters = facetFilters ?? new List<FacetFilter>();
            foreach (var facet in facets)
            {
                if (facet.Name is not null)
                {
                    var facetFilter = facetFilters
                        .FirstOrDefault(x => x.Name!.Equals(facet.Name, StringComparison.OrdinalIgnoreCase));

                    var count = string.Empty;
                    if (facetFilter != null && facetFilter.Count != 10)
                    {
                        count = $",count:{facetFilter?.Count}";
                    }
                    var sort = ",sort:count";
                    results.Add($"{facet.Name}{count}{sort}");
                }
            }
            return results;
        }

        private Azure.Search.Documents.SearchOptions GenerateSearchOptions(
            SearchDocumentsQuery request,
            Schema schema)
        {
            var options = new Azure.Search.Documents.SearchOptions()
            {
                SearchMode = SearchMode.All,
                Size = _searchOptions.PageSize,
                Skip = (request.Page - 1) * _searchOptions.PageSize,
                IncludeTotalCount = true,
                QueryType = SearchQueryType.Full,
                HighlightPreTag = "<b>",
                HighlightPostTag = "</b>",
            };

            
            foreach (string s in schema.SelectFilter)
            {
                options.Select.Add(s);
            }

            foreach (var facet in GenerateFacets(schema.Facets, request.FacetFilters))
            {
                options.Facets.Add(facet);
            }


            foreach (string h in schema.SearchableFields)
            {
                options.HighlightFields.Add(h);
            }

            // Filter Query
            var filterBuilder = new List<string>();
            GenerateFilters(filterBuilder, request.FacetFilters, schema.Facets);
            GenerateFilters(filterBuilder, request.FieldFilters, schema.Fields);
            if (filterBuilder.Count > 0)
                options.Filter = string.Join(" and ", filterBuilder);

            //Order by
            if (request.Order != null && 
                request.Order.Any(x => x.Name?.Equals("$orderby") ?? false))
            {
                var orderValues = request.Order
                    .Where(x => x.Name?.Equals("$orderby") ?? false)
                    .SelectMany(x => x.Values);
                foreach (var orderValue in orderValues)
                {
                    if(!string.IsNullOrWhiteSpace(orderValue))
                        options.OrderBy.Add(orderValue);
                }
            }


            // Add Filter based on geographic polygon if it is set.
            if (!string.IsNullOrWhiteSpace(request.PolygonString))
            {
                string geoQuery = $"geo.intersects(geoLocation, geography'POLYGON(({request.PolygonString}))')";

                if (options.Filter is not null && options.Filter.Length > 0)
                {
                    options.Filter += " and " + geoQuery;
                }
                else
                {
                    options.Filter = geoQuery;
                }
            }

            return options;
        }

        private void GenerateFilters(List<string> builder, IReadOnlyList<FacetFilter> filters, IReadOnlyCollection<SchemaField> schemaFields)
        {
            if (filters == null)
                return;

            foreach (var facetFilter in filters)
            {
                var facet = schemaFields.FirstOrDefault(f => f.Name == facetFilter.Name);
                if (facet == null)
                    continue;

                var facetValues = string.Join(",", facetFilter.Values);

                string? clause = default;

                var facetType = facet?.Type;

                if (facetType == typeof(string[]))
                {
                    clause = $"{facetFilter.Name}/any(t: search.in(t, '{facetValues}', ','))";
                }
                else if (facetType == typeof(string))
                {
                    if (facetFilter.Values.Count > 1)
                    {
                        var orFilters = facetFilter.Values.Select(x => $"{facetFilter.Name} eq '{x}'");
                        var query = string.Join(" or ", orFilters);
                        clause = $"({query})";
                    }
                    else
                    {
                        clause = $"{facetFilter.Name} eq '{facetValues}'";
                    }

                }
                else if (facetType == typeof(DateTime) || facetType == typeof(DateTimeOffset))
                {
                    clause = $"{facetFilter.Name} {facetValues}";
                }
                else continue;

                builder.Add(clause);
            }
        }
    }
}
