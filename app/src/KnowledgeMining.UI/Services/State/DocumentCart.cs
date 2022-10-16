using System.Text.Json.Serialization;

namespace KnowledgeMining.UI.Services.State
{
    public record DocumentCartEvent(CartAction Action, DocumentCartItem Item, IList<DocumentCartItem> Items);

    public class DocumentCartItem
    {
        [JsonPropertyName("id")]
        public string? RecordId { get; set; }
        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }
}
