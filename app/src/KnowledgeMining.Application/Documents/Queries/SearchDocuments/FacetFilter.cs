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

        public override bool Equals(object? obj)
        {
            if (obj != null && obj.GetType().IsSubclassOf(typeof(FacetFilter)))
            {
                var equals = true;
                var comparison = obj as FacetFilter;

                equals = Count == comparison!.Count;

                if ((Name == null && comparison.Name != null) ||
                    (Name != null && comparison.Name == null))
                    equals = false;
                else if (Name != null && !Name.Equals(comparison.Name))
                    equals = false;

                if (Values != null && comparison.Values != null)
                {
                    var values = string.Join(',', Values);
                    var compValues = string.Join(',', comparison.Values);
                    if (!values.Equals(compValues))
                        equals = false;
                }
                else if (Values != comparison.Values)
                    equals = false;

                return equals;
            }

            return false;
        }
    }
}
