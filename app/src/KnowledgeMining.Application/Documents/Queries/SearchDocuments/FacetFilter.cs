namespace KnowledgeMining.Application.Documents.Queries.SearchDocuments
{
    public class FacetFilter
    {
        public string? Name { get; set; }
        public IList<string> Values { get; set; }
        public Type? OverrideType { get; set; } = null;
        public int Count { get; set; } = 10;

        public FacetFilter()
        {
            Values = new List<string>();
        }
    }
}
