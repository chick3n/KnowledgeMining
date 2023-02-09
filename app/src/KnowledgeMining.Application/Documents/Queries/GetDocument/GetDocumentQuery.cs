using FluentValidation;
using KnowledgeMining.Application.Common.Interfaces;
using MediatR;

namespace KnowledgeMining.Application.Documents.Queries.GetDocument
{
    public record struct Document(string Name, IDictionary<string, string>? Tags, IDictionary<string, string>? Metadata = null, byte[]? rawContent = null);

    public readonly record struct GetDocumentResponse(Document Document);

    public readonly record struct GetDocumentQuery(string Key, string Container, string Filename, bool downloadContent = false) : IRequest<GetDocumentResponse>;

    public class GetDocumentsQueryValidator : AbstractValidator<GetDocumentQuery>
    {
        public GetDocumentsQueryValidator()
        {
            RuleFor(x => x.Key).NotEmpty();
            RuleFor(x => x.Container).NotEmpty();
            RuleFor(q => q.Filename).NotEmpty();
        }
    }

    public class GetDocumentsQueryHandler : IRequestHandler<GetDocumentQuery, GetDocumentResponse>
    {
        private readonly IStorageService _storageService;

        public GetDocumentsQueryHandler(IStorageService storageService)
        {
            _storageService = storageService;
        }

        public async Task<GetDocumentResponse> Handle(GetDocumentQuery request, CancellationToken cancellationToken)
        {
            return await _storageService.GetDocument(request.Key, request.Container, request.Filename, request.downloadContent, cancellationToken);
        }
    }
}
