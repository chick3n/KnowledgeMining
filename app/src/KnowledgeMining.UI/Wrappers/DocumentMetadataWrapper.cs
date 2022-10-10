using KnowledgeMining.Domain.Entities;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace KnowledgeMining.UI.Wrappers
{
    public class DocumentMetadataWrapper
    {

        public DocumentMetadataWrapper(
            IEnumerable<DocumentMetadata> documentMetadata, IndexItemFieldMapping indexItemFieldMapping, string? keyFieldName,
            string? searchQuery = null)
        {
            DocumentMetadata = documentMetadata;
            IndexItemFieldMapping = indexItemFieldMapping;
            KeyFieldName = keyFieldName;
            RelevantWords = TokenizeRelevantWords(searchQuery);
        }

        public IEnumerable<DocumentMetadata> DocumentMetadata { get; set; }
        public IndexItemFieldMapping IndexItemFieldMapping { get; set; } = new IndexItemFieldMapping();
        public string? KeyFieldName { get; set; }
        public string[]? RelevantWords { get; set; }

        private string[]? TokenizeRelevantWords(string query)
        {
            if (string.IsNullOrEmpty(query))
                return null;

            query = query.ToLower()
                .Replace("*", string.Empty)
                .Replace(" or ", string.Empty)
                .Replace(" and ", string.Empty)
                .Replace(" not ", string.Empty);

            return new string(query.ToCharArray().Where(x => !char.IsPunctuation(x)).ToArray())
                .Split(' ');
        }

        public IEnumerable<DocumentMetadata> Documents()
        {
            return DocumentMetadata.Select(x => ToDocumentMetadata(x)).ToList();
        }

        private DocumentMetadata ToDocumentMetadata(DocumentMetadata d)
        {
            var documentMetadata = new DocumentMetadata();

            if(d != null)
            {
                documentMetadata.ExtensionData = d.ExtensionData;
                documentMetadata.Locations = GetValue(d.Locations, d.ExtensionData, "locations");
                documentMetadata.SourcePath = GetValue(d.SourcePath, d.ExtensionData, "sourcePath");
                documentMetadata.Organizations = GetValue(d.Organizations, d.ExtensionData, "organizations");
                documentMetadata.Content = GetValue(d.Content, d.ExtensionData, "content");
                documentMetadata.DateTime = GetValue(d.DateTime, d.ExtensionData, "datetime");
                documentMetadata.KeyPhrases = GetValue(d.KeyPhrases, d.ExtensionData, "keyPhrases");
                documentMetadata.Persons = GetValue(d.Persons, d.ExtensionData, "persons");
                documentMetadata.SearchScore = GetValue(d.SearchScore, d.ExtensionData, "@search.score");
                documentMetadata.SourceType = GetValue(d.SourceType, d.ExtensionData, "sourceType");
                documentMetadata.Summary = GetValue(d.Summary, d.ExtensionData, "summary");
                documentMetadata.Text = GetValue(d.Text, d.ExtensionData, "text");
                documentMetadata.Title = GetValue(d.Title, d.ExtensionData, "title");
                documentMetadata.Name = GetValue(d.Name, d.ExtensionData, "name");
                documentMetadata.Category = GetValue(d.Category, d.ExtensionData, "category");
                documentMetadata.Topics = GetValue(d.Topics, d.ExtensionData, "topics");
                documentMetadata.Id = d.GetKeyValue(KeyFieldName);
                documentMetadata.IconUrl = GetValue(d.IconUrl, d.ExtensionData, "icon");
            }

            return documentMetadata;
        }

        private bool TryGetMappedValue<T>(string? fieldName, Dictionary<string, JsonElement> keyValuePairs, out T? value)
        {
            if (!string.IsNullOrWhiteSpace(fieldName))
            {
                if (keyValuePairs != null && keyValuePairs.ContainsKey(fieldName))
                {
                    if (keyValuePairs.TryGetValue(fieldName, out JsonElement element))
                    {
                        try
                        {
                            value = element.Deserialize<T>();
                            return true;
                        } 
                        catch
                        {

                        }
                    }
                }
            }

            value = default;
            return false;
        }

        private T? GetValue<T>(T? value, Dictionary<string, JsonElement>? keyValuePairs, string? map)
        {
            if(map != null && keyValuePairs != null)
            {
                if (TryGetMappedValue<T>(IndexItemFieldMapping.GetMappedField(map), keyValuePairs, out T? mappedValue))
                    return mappedValue;
            }
            return value;
        }

        public IEnumerable<string> GetCloudWords(DocumentMetadata d)
        {
            if (d.KeyPhrases != null && d.KeyPhrases.Count() > 0)
                return d.KeyPhrases;

            if (TryGetMappedValue<IEnumerable<string>>(IndexItemFieldMapping.GetMappedField(IndexItemFieldNames.WordCloud),
                    d.ExtensionData, out var words))
                return words;

            return Enumerable.Empty<string>();
        }

        public string? GetTitle(DocumentMetadata d)
        {
            if (!string.IsNullOrWhiteSpace(d.Title))
                return d.Title;

            if (TryGetMappedValue<string>(IndexItemFieldMapping.GetMappedField(IndexItemFieldNames.AltTitle),
                    d.ExtensionData, out var altTitle))
                return altTitle;

            return string.Empty;
        }

        public string? GetRelevantDocumentContent(DocumentMetadata document, int length)
        {
            if (RelevantWords == null || document == null)
                return null;

            if (RelevantWords.Length == 0)
                return null;

            if(!string.IsNullOrEmpty(document.Content))
            {
                if (document.Content.Length <= length)
                    return document.Content;

                var indexes = new List<(int, int)>();

                foreach(var token in RelevantWords)
                {
                    var tokenIndexes = GetRelevantIndexes(document.Content, token);
                    if (tokenIndexes != null)
                        indexes.AddRange(tokenIndexes);
                }

                var range = GetRelevantRangeOfContent(length, indexes.ToArray()); 
                if(range != null && range.Value.Item1 <= document.Content.Length && range.Value.Item2 <= document.Content.Length)
                {
                    return document.Content.Substring(range.Value.Item1, (range.Value.Item2 - range.Value.Item1));
                }
            }

            return null;
        }

        private (int,int)[]? GetRelevantIndexes(string content, string token)
        {
            var indexes = new List<(int, int)>();

            var lastIndex = 0;
            do
            {
                var index = content.IndexOf(token, lastIndex);
                if (index > -1)
                    indexes.Add((index, token.Length));
                else break;

                lastIndex = index+1;
            } while (lastIndex > -1);

            return indexes.Count > 0 ?
                indexes.ToArray() : null;
        }

        private (int, int)? GetRelevantRangeOfContent(int contentLength, (int, int)[] positions)
        {
            var orderedPositions = positions.OrderBy(x => x.Item1);
            (int, int, int)? bestMatch = null;

            for(var x=0; x<positions.Length; x++)
            {
                var position = positions[x];
                var tokenCount = 0;
                var start = position.Item1;
                var end = start;

                for(var y=x; y<positions.Length; y++)
                {
                    var position2 = positions[y];
                    end = position2.Item1 + position2.Item2;

                    if ((end - start) < contentLength)
                        tokenCount++;
                    else
                        break;
                }

                if (bestMatch == null || (bestMatch != null && bestMatch.Value.Item3 < tokenCount))
                    bestMatch = (start, end, tokenCount);
            }

            if(bestMatch == null)
                return null;

            return (bestMatch.Value.Item1, bestMatch.Value.Item2);
        }
    }
}
