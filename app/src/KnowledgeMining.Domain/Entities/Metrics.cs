using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KnowledgeMining.Domain.Entities
{
    public class Metrics
    {
        [JsonPropertyName("index")]
        public string? Index { get; set; }

        [JsonPropertyName("words")]
        public WordCount? WordCount { get; set; }
    }
}
