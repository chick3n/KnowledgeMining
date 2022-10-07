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
        [JsonPropertyName("icon")]
        public string? IconUrl { get; set; }

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

        public string? GetValue(string key, string seperator = ",", string dateTimeFormat = "yyyy-MM-dd")
        {
            dateTimeFormat = dateTimeFormat ?? "yyyy-MM-dd";
            seperator = seperator ?? ",";

            switch(key)
            {
                case "@search.score":
                    return SearchScore.ToString();
                case "id":
                    return Id;
                case "keyPhrases":
                    return KeyPhrases != null ? string.Join(seperator, KeyPhrases) : null;
                case "organizations":
                    return Organizations != null ? string.Join(seperator, Organizations) : null;
                case "persons":
                    return Persons != null ? string.Join(seperator, Persons) : null;
                case "locations":
                    return Locations != null ? string.Join(seperator, Locations) : null;
                case "topics":
                    return Topics != null ? string.Join(seperator, Topics) : null;
                case "text":
                    return Text != null ? string.Join(seperator, Text) : null;
                case "content":
                    return Content;
                case "layoutText":
                    return Topics != null ? string.Join(seperator, Topics) : null;
                case "imageTags":
                    return Topics != null ? string.Join(seperator, Topics) : null;
                case "merged_content":
                    return MergedContent;
                case "summary":
                    return Summary;
                case "datetime":
                    return DateTime?.ToString(dateTimeFormat);
                case "title":
                    return Title;
                case "sourceType":
                    return SourceType;
                case "sourcePath":
                    return SourcePath;
                case "name":
                    return Name;
                case "category":
                    return Category;
                case "icon":
                    return IconUrl;
            }

            if (ExtensionData != null && ExtensionData.ContainsKey(key))
                return ExtensionData[key].ToString();

            return null; 
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
                    dict.TryAdd(kvp.Key, kvp.Value.ToString());
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
