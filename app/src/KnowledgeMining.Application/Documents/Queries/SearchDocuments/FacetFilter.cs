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
            FacetFilter comparison = null;

            try
            {
                comparison = obj as FacetFilter;
            }
            catch
            {
                return false;
            }

            if (obj != null && obj.GetType().IsSubclassOf(typeof(FacetFilter)))
            {
                var equals = true;

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
