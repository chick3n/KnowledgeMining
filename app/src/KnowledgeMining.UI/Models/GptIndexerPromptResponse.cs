namespace KnowledgeMining.UI.Models
{
    public class GptIndexerPromptResponse
    {
        public Solution solution { get; set; }
    }

    public class Solution
    {
        public string response { get; set; }
        public Source_Nodes[] source_nodes { get; set; }
        public object extra_info { get; set; }
    }

    public class Source_Nodes
    {
        public string source_text { get; set; }
        public string doc_id { get; set; }
        public object extra_info { get; set; }
        public Node_Info node_info { get; set; }
        public float similarity { get; set; }
        public object image { get; set; }
    }

    public class Node_Info
    {
        public int start { get; set; }
        public int end { get; set; }
    }

}
