using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KnowledgeMining.Domain.Entities
{
    public class MetricsSearch
    {
        [JsonPropertyName("document_count")]
        public int? DocumentCount { get; set; }

        [JsonPropertyName("index_size")]
        public string? IndexSize { get; set; }

        [JsonPropertyName("file_types")]
        public Dictionary<string, int>? FileTypes { get; set; }
    }
}
