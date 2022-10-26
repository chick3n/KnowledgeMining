using FluentValidation;
using KnowledgeMining.Application.Common.Interfaces;
using MediatR;

namespace KnowledgeMining.Application.Documents.Queries.GetDocumentContent
{
    public record struct DocumentContent(string Name, IDictionary<string, string>? Tags, string Content);

    public readonly record struct GetDocumentContentResponse(DocumentContent Document);

    public readonly record struct GetDocumentContentQuery(string FileName) : IRequest<GetDocumentContentResponse>;

    public class GetDocumentsQueryValidator : AbstractValidator<GetDocumentContentQuery>
    {
        public GetDocumentsQueryValidator()
        {
            RuleFor(q => q.FileName)
                .NotEmpty()
                .WithMessage("Filename must not be empty.");
        }
    }

    public class GetDocumentsQueryHandler : IRequestHandler<GetDocumentContentQuery, GetDocumentContentResponse>
    {
        private readonly IStorageService _storageService;

        public GetDocumentsQueryHandler(IStorageService storageService)
        {
            _storageService = storageService;
        }

        public async Task<GetDocumentContentResponse> Handle(GetDocumentContentQuery request, CancellationToken cancellationToken)
        {
            var bytes = await _storageService.DownloadDocument(request.FileName, cancellationToken);
            if (bytes != null)
            {
                return new GetDocumentContentResponse
                {
                    Document = new DocumentContent
                    {
                        Name = request.FileName,
                        Content = System.Text.Encoding.UTF8.GetString(bytes),
                        Tags = null
                    }
                };
            }

            throw new FileNotFoundException(request.FileName);
        }
    }
}
