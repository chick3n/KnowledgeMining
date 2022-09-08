using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.Domain.Entities;
using MediatR;

namespace KnowledgeMining.Application.Documents.Queries.SearchDocuments
{
    public class SearchDocumentsResponse
    {
        public IEnumerable<DocumentMetadata> Documents { get; set; } = Enumerable.Empty<DocumentMetadata>();
        public IEnumerable<SummarizedFacet> Facets { get; set; } = Enumerable.Empty<SummarizedFacet>();
        public long TotalPages { get; set; }
        public IEnumerable<string?> FacetableFields { get; set; } = Enumerable.Empty<string?>();
        public IEnumerable<string?> FilterableFields { get; set; } = Enumerable.Empty<string?>();
        public long TotalCount { get; set; }
        public string? SearchId { get; set; }
    }

    public class SearchDocumentsQuery : IRequest<SearchDocumentsResponse>
    {
        public SearchDocumentsQuery(IndexItem index,
                             string query,
                             int page,
                             string polygonString,
                             IEnumerable<FacetFilter> facetFilters,
                             IEnumerable<FacetFilter> fieldFilters,
                             IEnumerable<FacetFilter> order)
        {
            Index = index;
            SearchText = string.IsNullOrWhiteSpace(query) ? "*" : query.Replace("?", string.Empty);
            FacetFilters = (facetFilters ?? Array.Empty<FacetFilter>()).ToList().AsReadOnly();
            FieldFilters = (fieldFilters ?? Array.Empty<FacetFilter>()).ToList().AsReadOnly();
            Page = page > 0 ? page : 1;
            PolygonString = string.IsNullOrWhiteSpace(polygonString) ? string.Empty : polygonString;
            Order = (order ?? Array.Empty<FacetFilter>()).ToList().AsReadOnly();
        }

        public IndexItem Index { get; private set; }
        public string SearchText { get; private set; }
        public IReadOnlyList<FacetFilter> FacetFilters { get; private set; }
        public IReadOnlyList<FacetFilter> FieldFilters { get; private set; }
        public int Page { get; private set; }
        public string PolygonString { get; private set; }
        public IReadOnlyList<FacetFilter> Order { get; private set; }
    }

    public class SearchDocumentsQueryHandler : IRequestHandler<SearchDocumentsQuery, SearchDocumentsResponse>
    {
        private readonly ISearchService _searchService;

        public SearchDocumentsQueryHandler(ISearchService searchService)
        {
            _searchService = searchService;
        }

        public async Task<SearchDocumentsResponse> Handle(SearchDocumentsQuery request, CancellationToken cancellationToken)
        {
            return await _searchService.SearchDocuments(request, cancellationToken);
        }
    }
}
