using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KnowledgeMining.Domain.Entities
{
    public class IndexItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        [JsonPropertyName("index")]
        public string? IndexName { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("route")]
        public string? Route { get; set; }
        [JsonPropertyName("navigation")]
        public IEnumerable<IndexNavigationItem>? NavigationItems { get; set; }
        [JsonPropertyName("fieldMapping")]
        public IndexItemFieldMapping? FieldMapping { get; set; }
    }
}
