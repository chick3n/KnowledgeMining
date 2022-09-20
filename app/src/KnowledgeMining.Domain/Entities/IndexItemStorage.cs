using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KnowledgeMining.Domain.Entities
{
    public class IndexItemStorage
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }
        [JsonPropertyName("container")]
        public string? Container { get; set; }
        [JsonPropertyName("allowSync")]
        public bool AllowSync { get; set; } = false;
    }
}
