using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KnowledgeMining.Domain.Entities
{
    public class IndexItemErrorHint
    {
        [JsonPropertyName("EN")]
        public string? EnglishString { get; set; }

        [JsonPropertyName("FR")]
        public string? FrenchString { get; set; }
    }
}
