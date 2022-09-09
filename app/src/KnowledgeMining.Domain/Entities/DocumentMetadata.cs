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
            return new Dictionary<string, object?>()
            {
                { ToLowerFirstChar(nameof(KeyPhrases)), KeyPhrases },
                { ToLowerFirstChar(nameof(Organizations)), Organizations },
                { ToLowerFirstChar(nameof(Persons)), Persons },
                { ToLowerFirstChar(nameof(Locations)), Locations },
                { ToLowerFirstChar(nameof(Topics)), Topics },
                { ToLowerFirstChar(nameof(Text)), Text },
                { ToLowerFirstChar(nameof(Summary)), Summary },
                { ToLowerFirstChar(nameof(MergedContent)), MergedContent },
                { ToLowerFirstChar(nameof(Title)), Title },
                { ToLowerFirstChar(nameof(SourceType)), SourceType },
                { ToLowerFirstChar(nameof(SourcePath)), SourcePath }
            };
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
