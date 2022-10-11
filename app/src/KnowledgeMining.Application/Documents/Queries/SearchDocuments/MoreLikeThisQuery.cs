using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.Domain.Entities;
using MediatR;

namespace KnowledgeMining.Application.Documents.Queries.SearchDocuments
{
    public class MoreLikeThisResponse
    {
        public IEnumerable<DocumentMetadata> Documents { get; set; } = Enumerable.Empty<DocumentMetadata>();
    }

    public record MoreLikeThisQuery(string IndexName, string Key, string[]? Select = null, string[]? SearchOn = null) : IRequest<MoreLikeThisResponse>;

    public class MoreLikeThisQueryHandler : IRequestHandler<MoreLikeThisQuery, MoreLikeThisResponse>
    {
        private readonly ISearchService _searchService;

        public MoreLikeThisQueryHandler(ISearchService searchService)
        {
            _searchService = searchService;
        }

        public async Task<MoreLikeThisResponse> Handle(MoreLikeThisQuery request, CancellationToken cancellationToken)
        {
            var results = await _searchService.MoreLikeThis(request.IndexName,
                request.Key, 
                cancellationToken);

            return new MoreLikeThisResponse()
            {
                Documents = results.Values
            };
        }
    }
}
