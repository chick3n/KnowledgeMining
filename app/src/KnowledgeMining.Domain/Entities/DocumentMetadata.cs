using System.Text.Json;
using System.Text.Json.Serialization;

namespace KnowledgeMining.Domain.Entities
{
    public class DocumentMetadata
    {
        [JsonPropertyName("@search.score")]
        public double SearchScore { get; set; }
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        [JsonPropertyName("keyPhrases")]
        public IEnumerable<string>? KeyPhrases { get; set; }
        [JsonPropertyName("organizations")]
        public IEnumerable<string>? Organizations { get; set; }
        [JsonPropertyName("persons")]
        public IEnumerable<string>? Persons { get; set; }
        [JsonPropertyName("locations")]
        public IEnumerable<string>? Locations { get; set; }
        [JsonPropertyName("topics")]
        public IEnumerable<string>? Topics { get; set; }
        [JsonPropertyName("text")]
        public IEnumerable<string>? Text { get; set; }
        [JsonPropertyName("content")]
        public string? Content { get; set; }
        [JsonPropertyName("layoutText")]
        public IEnumerable<string>? LayoutText { get; set; }
        [JsonPropertyName("imageTags")]
        public IEnumerable<string>? ImageTags { get; set; }
        [JsonPropertyName("merged_content")]
        public string? MergedContent { get; set; }
        [JsonPropertyName("summary")]
        public string? Summary { get; set; }
        [JsonPropertyName("datetime")]
        public DateTime? DateTime { get; set; }
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        [JsonPropertyName("sourceType")]
        public string? SourceType { get; set; }
        [JsonPropertyName("sourcePath")]
        public string? SourcePath { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }


        public string GetKeyValue(string? keyField = null)
        {
            if(keyField != null && Id == null)
            {
                if(ExtensionData != null && ExtensionData.TryGetValue(keyField, out var value))
                {
                    return value.ToString();
                }
            }

            return Id;
        }

        public IDictionary<string, object?> ToDictionary()
        {
            var dict = new Dictionary<string, object?>()
            {
                { ToLowerFirstChar(nameof(KeyPhrases)), KeyPhrases },
                { ToLowerFirstChar(nameof(Organizations)), Organizations },
                { ToLowerFirstChar(nameof(Persons)), Persons },
                { ToLowerFirstChar(nameof(Locations)), Locations },
                { ToLowerFirstChar(nameof(Topics)), Topics },
                { ToLowerFirstChar(nameof(Summary)), Summary },
                { ToLowerFirstChar(nameof(Title)), Title },
                { ToLowerFirstChar(nameof(SourceType)), SourceType },
                { ToLowerFirstChar(nameof(SourcePath)), SourcePath },
                { ToLowerFirstChar(nameof(Name)), Name },
                { ToLowerFirstChar(nameof(Category)), Category},
            };

            if(ExtensionData != null)
            {
                foreach (var kvp in ExtensionData)
                    dict.Add(kvp.Key, kvp.Value.ToString());
            }

            return dict.Where(x => MetadataHasValue(x.Value)).ToDictionary(x => x.Key, x => x.Value);
        }

        public bool IsMetadataValueAnArray(object? value)
        {
            if (value is not null)
            {
                if (value is IEnumerable<string>)
                {
                    return true;
                }
            }
            return false;
        }

        private bool MetadataHasValue(object? value)
        {
            if(IsMetadataValueAnArray(value))
            {
                var list = value as IEnumerable<string>;
                return list.Count() > 0;
            }
            return value != null;
        }

        public string ConvertMetadataValueToString(object? metadataValue)
        {
            if (metadataValue is not null)
            {
                if (metadataValue is IEnumerable<string> values)
                {
                    return string.Join("\n", values);
                }
                else
                {
                    return metadataValue!.ToString();
                }
            }

            return string.Empty;
        }

        // Took from https://stackoverflow.com/questions/21755757/first-character-of-string-lowercase-c-sharp
        private string ToLowerFirstChar(string str)
        {
            if (!string.IsNullOrEmpty(str) && char.IsUpper(str[0]))
                return str.Length == 1 ? char.ToLower(str[0]).ToString() : char.ToLower(str[0]) + str[1..];

            return str;
        }
    }
}
