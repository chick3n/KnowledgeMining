using KnowledgeMining.Domain.Entities;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace KnowledgeMining.UI.Wrappers
{
    public class DocumentMetadataWrapper
    {

        public DocumentMetadataWrapper(
            IEnumerable<DocumentMetadata> documentMetadata, IndexItemFieldMapping indexItemFieldMapping, string? keyFieldName)
        {
            DocumentMetadata = documentMetadata;
            IndexItemFieldMapping = indexItemFieldMapping;
            KeyFieldName = keyFieldName;
        }

        public IEnumerable<DocumentMetadata> DocumentMetadata { get; set; }
        public IndexItemFieldMapping IndexItemFieldMapping { get; set; } = new IndexItemFieldMapping();
        public string? KeyFieldName { get; set; }

        public IEnumerable<DocumentMetadata> Documents()
        {
            return DocumentMetadata.Select(x => ToDocumentMetadata(x)).ToList();
        }

        public bool HasDocuments => DocumentMetadata.Any();

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
                documentMetadata.Highlights = GetValue(d.Highlights, d.ExtensionData, "@search.hightlights");
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

        
    }
}
