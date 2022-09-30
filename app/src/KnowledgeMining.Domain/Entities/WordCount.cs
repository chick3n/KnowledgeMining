using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KnowledgeMining.Domain.Entities
{
    public class WordCount
    {
        [JsonPropertyName("count")]
        public int? Count { get; set; }

        [JsonPropertyName("count_excluded")]
        public int? CountExcludeStopWords { get; set; }

        [JsonPropertyName("top")]
        public Dictionary<string, int>? Top { get; set; }

        [JsonPropertyName("top_excluded")]
        public Dictionary<string, int>? TopExcludeStopWords { get; set; }

    }
}
