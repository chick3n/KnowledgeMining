using KnowledgeMining.UI.Services.State;
using System.Text.Json.Serialization;

namespace KnowledgeMining.UI.Pages.Services.Models
{
    public class DocumentServiceRequest
    {
        [JsonPropertyName("name")]
        public string? Id { get; set; }

        [JsonPropertyName("index_config")]
        public string? IndexConfig { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("documents")]
        public IEnumerable<DocumentCartItem> Documents { get; set; } = Enumerable.Empty<DocumentCartItem>();

    }
}
