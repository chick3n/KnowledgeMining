using KnowledgeMining.Application.Documents.Queries.SearchDocuments;
using System.Text;
using System.Text.Json.Serialization;

namespace KnowledgeMining.UI.Services.State
{
    public class SearchQueryStateModel
    {
        private const string Key = "ser";
        private const string SearchTextKey = "s";
        private const string PageKey = "p";
        private const string PolygonStringKey = "pp";
        private const string FacetFiltersKey = "fa";
        private const string FieldFiltersKey = "f";
        private const string OrderKey = "o";

        private const char Sep = '|';
        private const char Group = ':';
        private const char Series = ';';
        private const char SubGroup = '=';
        private const char MultiSep = ',';

        public string? SearchText { get; set; }
        public int Page { get; set; }
        public string? PolygonString { get; set; }
        public IEnumerable<FacetFilter> FacetFilters { get; set; } = new List<FacetFilter>();
        public IEnumerable<FacetFilter> FieldFilters { get; set; } = new List<FacetFilter>();
        public IEnumerable<FacetFilter> Order { get; set; } = new List<FacetFilter>();

        public static StateModel Hash(SearchDocumentsQuery query)
        {
            _ = query ?? throw new ArgumentNullException(nameof(query));

            List<string> values = new List<string>();

            if(!string.IsNullOrWhiteSpace(query.SearchText))
            {
                values.Add($"{SearchTextKey}{Group}{Uri.EscapeDataString(query.SearchText)}");
            }

            if(query.Page > 0)
            {
                values.Add($"{PageKey}{Group}{query.Page}");
            }

            if(!string.IsNullOrWhiteSpace(query.PolygonString))
            {
                values.Add($"{PolygonStringKey}{Group}{query.PolygonString}");
            }

            if(query.FacetFilters != null && query.FacetFilters.Count > 0)
            {
                values.Add(EncodeFacetFilters(FacetFiltersKey, query.FacetFilters));
            }

            if (query.FieldFilters != null && query.FieldFilters.Count > 0)
            {
                values.Add(EncodeFacetFilters(FieldFiltersKey, query.FieldFilters));
            }

            if (query.Order != null && query.Order.Count > 0)
            {
                values.Add(EncodeFacetFilters(OrderKey, query.Order));
            }

            return new StateModel(Key, string.Join(Sep, values));
        }

        private static string EncodeFacetFilters(string key, IEnumerable<FacetFilter> facetFilters)
        {
            var values = new List<string>();
            foreach(var facetFilter in  facetFilters)
            {
                values.Add(EncodeFacetFilter(facetFilter));
            }

            return $"{key}{Group}{string.Join(Series, values)}";
        }

        private static string EncodeFacetFilter(FacetFilter facetFilter)
        {
            if (facetFilter == null)
                return string.Empty;

            var values = facetFilter.Values.Select(x => Uri.EscapeDataString(x)).ToList();
            return $"{facetFilter.Name}{SubGroup}{string.Join(MultiSep, values)}";
        }

        public SearchQueryStateModel(StateModel model)
        {
            var values = model.Values.Split(Sep);

            foreach (var value in values)
            {
                var kvp = value.Split(Group);
                if (kvp.Length < 2)
                    continue;

                var key = kvp[0];
                var content = kvp[1];
                string subcontent = null;
                if (kvp.Length > 2)
                    subcontent = kvp[2];

                Set(key, content);
            }
        }

        private IEnumerable<FacetFilter> DecodeFacetFilters(string content)
        {
            List<FacetFilter> facetFilters = new();
            var series = content.Split(Series);
            foreach(var single in series)
            {
                var kvp = single.Split(SubGroup);
                if(kvp.Length >= 2)
                {
                    FacetFilter facetFilter = new FacetFilter();
                    facetFilter.Name = kvp[0];

                    var values = kvp[1].Split(MultiSep);
                    facetFilter.Values = values?.Select(x => Uri.UnescapeDataString(x)).ToList() ?? new();
                    facetFilters.Add(facetFilter);
                }
            }

            return facetFilters;
        }

        private void Set(string key, string content)
        {
            if (string.IsNullOrEmpty(key))
                return;

            switch(key)
            {
                case SearchQueryStateModel.SearchTextKey:
                    SearchText = Uri.UnescapeDataString(content);
                    break;
                case SearchQueryStateModel.PageKey:
                    if(int.TryParse(content, out var page))
                        Page = page;
                    break;
                case SearchQueryStateModel.PolygonStringKey:
                    PolygonString = Uri.UnescapeDataString(content);
                    break;
                case SearchQueryStateModel.FacetFiltersKey:
                    FacetFilters = DecodeFacetFilters(content);
                    break;
                case SearchQueryStateModel.FieldFiltersKey:
                    FieldFilters = DecodeFacetFilters(content);
                    break;
                case SearchQueryStateModel.OrderKey:
                    Order = DecodeFacetFilters(content);
                    break;
            }
        }
    }
}
