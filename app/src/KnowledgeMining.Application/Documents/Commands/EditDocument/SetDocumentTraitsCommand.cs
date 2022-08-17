using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.Application.Documents.Queries.GetDocuments;
using KnowledgeMining.Domain.Enums;
using MediatR;

namespace KnowledgeMining.Application.Documents.Commands.EditDocument
{
    public readonly record struct SetDocumentTraitsCommand(Document Document, DocumentTraits Traits) : IRequest<bool>;

    public class SetDocumentTagsCommandHandler : IRequestHandler<SetDocumentTraitsCommand, bool>
    {
        private readonly IStorageService _storageService;

        public SetDocumentTagsCommandHandler(IStorageService storageService)
        {
            _storageService = storageService;
        }

        public async Task<bool> Handle(SetDocumentTraitsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _storageService.SetDocumentTraits(request.Document, request.Traits, cancellationToken);
            }
            catch
            {
                return false;
            }
            
            return true;
        }
    }
}
