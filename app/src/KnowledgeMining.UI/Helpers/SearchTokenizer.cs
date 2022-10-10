namespace KnowledgeMining.UI.Helpers
{
    public class SearchTokenizer
    {
        private string? _originalSearchQuery;
        private string _searchQuery = string.Empty;

        public string[] Tokens { get; private set; } = new string[0];

        public SearchTokenizer(string? query)
        {
            _originalSearchQuery = query;
            if (query != null)
            {
                _searchQuery = CleanQuery(query);
                Tokens = GenerateSearchQueryTokens();
            }
        }

        public bool HasTokens
        {
            get
            {
                return Tokens.Length > 0;
            }
        }

        private string CleanQuery(string? query)
        {
            if (query == null)
                return string.Empty;

            var cleanText = query.ToLower()
                .Replace("*", string.Empty)
                .Replace(" or ", string.Empty)
                .Replace(" and ", string.Empty)
                .Replace(" not ", string.Empty);

            return cleanText;
        }

        private string[] GenerateSearchQueryTokens()
        {
            var tokens = new string(_searchQuery.ToCharArray().Where(x => !char.IsPunctuation(x)).ToArray())
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return tokens;
        }
    }
}
