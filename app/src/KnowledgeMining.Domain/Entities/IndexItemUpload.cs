using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KnowledgeMining.Domain.Entities
{
    public class IndexItemUpload
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }
        
        [JsonPropertyName("container")]
        public string? Container { get; set; }

        [JsonPropertyName("disabled")]
        public bool Disabled { get; set; } = true;

        [JsonPropertyName("metadata")]
        public IEnumerable<IndexItemUploadMetadata>? Metadata { get; set; }
    }

    public class IndexItemUploadMetadata
    {
        [JsonPropertyName("name")]
        public Localization? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("required")]
        public bool Required { get; set; } = false; 

        [JsonPropertyName("values")]
        public IEnumerable<IndexItemUploadMetadataValues>? Values { get; set; }
    }

    public class IndexItemUploadMetadataValues
    {
        [JsonPropertyName("label")]
        public Localization? Label { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }
}
