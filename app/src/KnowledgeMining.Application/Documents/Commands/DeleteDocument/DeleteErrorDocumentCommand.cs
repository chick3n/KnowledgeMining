using FluentValidation;
using KnowledgeMining.Application.Common.Interfaces;
using MediatR;

namespace KnowledgeMining.Application.Documents.Commands.DeleteDocument
{
    public readonly record struct DeleteErrorDocumentCommand(string DocumentName, string? DocumentContainer = null, string? Key = null) : IRequest<Unit>;

    public class DeleteErrorDocumentCommandValidator : AbstractValidator<DeleteErrorDocumentCommand>
    {
        public DeleteErrorDocumentCommandValidator()
        {
            RuleFor(x => x.DocumentName).NotEmpty().WithMessage("Document name must be provided.");
        }
    }

    public class DeleteErrorDocumentCommandHandler : IRequestHandler<DeleteErrorDocumentCommand, Unit>
    {
        private readonly IStorageService _storageService;

        public DeleteErrorDocumentCommandHandler(IStorageService storageService, ISearchService searchService)
        {
            _storageService = storageService;
        }

        public async Task<Unit> Handle(DeleteErrorDocumentCommand request, CancellationToken cancellationToken)
        {

            if (request.Key != null && request.DocumentContainer != null)
            {
                await _storageService.DeleteDocument(request.Key, request.DocumentContainer, request.DocumentName, cancellationToken);
            }
            else
            {
                await _storageService.DeleteDocument(request.DocumentName, cancellationToken);
            }
            

            return Unit.Value;
        }
    }
}