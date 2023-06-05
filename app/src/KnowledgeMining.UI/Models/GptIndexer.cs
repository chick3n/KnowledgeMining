namespace KnowledgeMining.UI.Models
{
    public class GptIndexerPromptRequest
    {
        public string? Name { get; set; }
        public string? Input { get; set; }

        public List<string> Documents { get; set; } = new List<string>();
    }
}
