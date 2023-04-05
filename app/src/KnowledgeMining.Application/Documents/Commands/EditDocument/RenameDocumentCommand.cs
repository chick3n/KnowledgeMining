using FluentValidation;
using KnowledgeMining.Application.Common.Interfaces;
using MediatR;

namespace KnowledgeMining.Application.Documents.Commands.DeleteDocument
{
    public readonly record struct RenameDocumentCommand(string? Key, string? Container, string? CurrentName, string? NewName) : IRequest<Unit>;

    public class RenameDocumentCommandValidator : AbstractValidator<RenameDocumentCommand>
    {
        public RenameDocumentCommandValidator()
        {
            RuleFor(x => x.Key).NotEmpty().WithMessage("Storage key must be provided.");
            RuleFor(x => x.Container).NotEmpty().WithMessage("Storage container must be provided.");
            RuleFor(x => x.CurrentName).NotEmpty().WithMessage("Current blob name must be provided.");
            RuleFor(x => x.NewName).NotEmpty().WithMessage("New blob name must be provided.");
        }
    }

    public class RenameDocumentCommandHandler : IRequestHandler<RenameDocumentCommand, Unit>
    {
        private readonly IStorageService _storageService;

        public RenameDocumentCommandHandler(IStorageService storageService, ISearchService searchService)
        {
            _storageService = storageService;
        }

        public async Task<Unit> Handle(RenameDocumentCommand request, CancellationToken cancellationToken)
        {
            await _storageService.RenameDocument(request.Key, request.Container, request.CurrentName, request.NewName, cancellationToken);

            return Unit.Value;
        }
    }
}