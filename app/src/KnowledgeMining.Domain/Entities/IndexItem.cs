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
        public Localization? Name { get; set; }
        [JsonPropertyName("route")]
        public string? Route { get; set; }
        [JsonPropertyName("keyField")]
        public string? KeyField { get; set; }
        [JsonPropertyName("storage")]
        public IndexItemStorage? Storage { get; set; }
        [JsonPropertyName("logo")]
        public string? Logo { get; set; }
        [JsonPropertyName("navigation")]
        public IEnumerable<IndexNavigationItem>? NavigationItems { get; set; }
        [JsonPropertyName("fieldMapping")]
        public IndexItemFieldMapping? FieldMapping { get; set; }
        [JsonPropertyName("facets")]
        public IEnumerable<IndexItemFacet> Facets { get; set; } = Enumerable.Empty<IndexItemFacet>();
        [JsonPropertyName("configuration")]
        public IndexItemConfiguration? Configuration { get; set; }
        [JsonPropertyName("landing")]
        public IndexItemLanding? Landing { get; set; }
    }
}
