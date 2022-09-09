using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KnowledgeMining.Domain.Entities
{

    public class IndexItemFacet
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("showAll")]
        public bool ShowAll { get; set; } = false;

        [JsonPropertyName("join")]
        public string Join { get; set; } = "and";

        [JsonPropertyName("values")]
        public IEnumerable<IndexItemFacetValue>? Values { get; set; }
    }

    public class IndexItemFacetValue
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
