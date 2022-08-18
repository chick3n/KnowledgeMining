using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeMining.Application.Documents.Queries.SearchDocuments
{
    public class OrderByFacetFilter : FacetFilter
    {
        private const string FieldName = "$orderby";

        private OrderByFacetFilter() { }

        public static OrderByFacetFilter BestMatch()
        {
            return new OrderByFacetFilter
            {
                Name = FieldName,
                Values = new string[] { "search.score() desc" }
            };
        }

        public static OrderByFacetFilter Date()
        {
            return new OrderByFacetFilter
            {
                Name = FieldName,
                Values = new string[] { "datetime desc" }
            };
        }
    }
}
