using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KnowledgeMining.Domain.Entities.Jobs
{
    public class DocumentJobRequest
    {
        [JsonPropertyName("name")]
        public string? Id { get; set; }

        [JsonPropertyName("index")]
        public string? Index { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("createdBy")]
        public string? CreatedBy { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; } = "Unknown";

        [JsonPropertyName("createdOn")]
        public long? CreatedOn { get; set; } //DateTimeOffset.Ticks

        [JsonPropertyName("createdOnOffset")]
        public long? CreatedOnOffset { get; set; } //DateTimeOffset.Offset.Ticks

        [JsonPropertyName("documents")]
        public IEnumerable<DocumentItem> Documents { get; set; } = Enumerable.Empty<DocumentItem>();
    }
}
