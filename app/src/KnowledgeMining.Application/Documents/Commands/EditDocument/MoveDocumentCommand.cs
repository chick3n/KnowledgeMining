using FluentValidation;
using KnowledgeMining.Application.Common.Interfaces;
using MediatR;

namespace KnowledgeMining.Application.Documents.Commands.DeleteDocument
{
    public readonly record struct MoveDocumentCommand(string? Key, string? SourceContainer, string SourceName, string DestinationContainer, string DestinationName) : IRequest<Unit>;

    public class MoveDocumentCommandValidator : AbstractValidator<MoveDocumentCommand>
    {
        public MoveDocumentCommandValidator()
        {
            RuleFor(x => x.Key).NotEmpty().WithMessage("Storage Key must be provided.");
            RuleFor(x => x.SourceContainer).NotEmpty().WithMessage("Source Container must be provided.");
            RuleFor(x => x.SourceName).NotEmpty().WithMessage("Source Name must be provided.");
            RuleFor(x => x.DestinationContainer).NotEmpty().WithMessage("Destination Container must be provided.");
            RuleFor(x => x.DestinationName).NotEmpty().WithMessage("Destination Name must be provided.");
        }
    }

    public class MoveDocumentCommandHandler : IRequestHandler<MoveDocumentCommand, Unit>
    {
        private readonly IStorageService _storageService;

        public MoveDocumentCommandHandler(IStorageService storageService, ISearchService searchService)
        {
            _storageService = storageService;
        }

        public async Task<Unit> Handle(MoveDocumentCommand request, CancellationToken cancellationToken)
        {
            await _storageService.MoveDocument(request.Key, request.SourceContainer, request.SourceName, request.DestinationContainer, request.DestinationName, cancellationToken);
            
            return Unit.Value;
        }
    }
}