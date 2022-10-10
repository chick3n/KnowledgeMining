namespace KnowledgeMining.UI.Helpers
{
    public class SearchTextRelevance
    {
        private readonly SearchTokenizer _tokenizer;
        private readonly string _text;
        private List<Range> _ranges;

        public SearchTextRelevance(SearchTokenizer searchTokenizer, string content)
        {
            _ = searchTokenizer ?? throw new ArgumentNullException(nameof(SearchTokenizer));
            _ = content ?? throw new ArgumentNullException(nameof(content));

            _tokenizer = searchTokenizer;
            _text = content;
            _ranges = GetRelevantTextRanges();
        }


        private List<Range> GetRelevantTextRanges()
        {
            var ranges = new List<Range>();

            if (!_tokenizer.HasTokens)
                return ranges;

            foreach(var token in _tokenizer.Tokens)
            {
                var tokenRanges = FindAllIndexes(token);
                ranges.AddRange(tokenRanges);
            }

            return ranges;
        }

        private List<Range> FindAllIndexes(string token)
        { 
            var ranges = new List<Range>();

            var lastIndex = 0;
            do
            {
                var index = _text.IndexOf(token, lastIndex);
                if (index > -1)
                    ranges.Add(new Range(index, index + token.Length));
                else break;

                lastIndex = index + 1;
            } while (lastIndex > -1);

            return ranges;
        }

        private string Section(Range range)
        {
            var start = range.Start.Value <= _text.Length ? range.Start.Value : 0;
            var end = range.End.Value <= _text.Length ? range.End.Value : _text.Length;

            return _text.Substring(start, end);
        }

        public string? MostRelevantSection(int length, bool getDefaultSection = false)
        {
            if (_text.Length <= length)
                return _text;

            var range = FindRelevantRangeOfContent(length);
            if (range != null)
            {
                return Section(range.Value);
            }

            if(getDefaultSection)
                return Section(new Range(0, length));

            return null;
        }

        private Range? FindRelevantRangeOfContent(int contentLength)
        {
            var positions = _ranges.OrderBy(x => x.Start.Value)
                .Select(x => (x.Start.Value, x.End.Value))
                .ToArray();

            (int, int, int)? bestMatch = null;

            for (var x = 0; x < positions.Length; x++)
            {
                var position = positions[x];
                var tokenCount = 0;
                var start = position.Item1;
                var end = start;

                for (var y = x; y < positions.Length; y++)
                {
                    var position2 = positions[y];
                    end = position2.Item1 + position2.Item2;

                    if ((end - start) < contentLength)
                        tokenCount++;
                    else
                        break;
                }

                if (bestMatch == null || (bestMatch != null && bestMatch.Value.Item3 < tokenCount))
                    bestMatch = (start, end, tokenCount);
            }

            if (bestMatch == null)
                return null;

            return new Range(bestMatch.Value.Item1, bestMatch.Value.Item2);
        }
    }
}
