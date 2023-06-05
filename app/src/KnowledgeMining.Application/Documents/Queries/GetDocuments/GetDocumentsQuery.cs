﻿using FluentValidation;
using KnowledgeMining.Application.Common.Interfaces;
using MediatR;

namespace KnowledgeMining.Application.Documents.Queries.GetDocuments
{
    public record struct Document(string Name, IDictionary<string, string>? Tags, DateTimeOffset? LastModifiedOn = null, IDictionary<string, string>? Metadata = null);

    public readonly record struct GetDocumentsResponse(IEnumerable<Document> Documents, string? NextPage);

    public readonly record struct GetDocumentsQuery(string container, string? SearchPrefix, int PageSize, string? ContinuationToken, string? key = null) : IRequest<GetDocumentsResponse>;

    public class GetDocumentsQueryValidator : AbstractValidator<GetDocumentsQuery>
    {
        public GetDocumentsQueryValidator()
        {
            RuleFor(q => q.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(5_000).WithMessage("Page size must a positive number between 1 and 5.000");
        }
    }

    public class GetDocumentsQueryHandler : IRequestHandler<GetDocumentsQuery, GetDocumentsResponse>
    {
        private readonly IStorageService _storageService;

        public GetDocumentsQueryHandler(IStorageService storageService)
        {
            _storageService = storageService;
        }

        public async Task<GetDocumentsResponse> Handle(GetDocumentsQuery request, CancellationToken cancellationToken)
        {
            if (request.key == null)
            {
                return await _storageService.GetDocuments(request.container, request.SearchPrefix, request.PageSize, request.ContinuationToken, cancellationToken);
            }
            else
            {
                return await _storageService.GetDocuments(request.key, request.container, request.SearchPrefix, request.PageSize, request.ContinuationToken, cancellationToken);
            }
            
        }
    }
}
