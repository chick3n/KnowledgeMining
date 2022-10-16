using KnowledgeMining.Application.Common.Interfaces;
using SearchDocument = KnowledgeMining.Application.Documents.Queries.GetDocuments.Document;
using MediatR;

namespace KnowledgeMining.Application.Documents.Commands.UploadDocument
{
    public readonly record struct UploadDocumentCommand(string ContainerName, IEnumerable<Document> Documents) : IRequest<IEnumerable<SearchDocument>>;

    public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, IEnumerable<SearchDocument>>
    {
        private readonly IStorageService _storageService;

        public UploadDocumentCommandHandler(IStorageService storageService)
        {
            _storageService = storageService;
        }

        public async Task<IEnumerable<SearchDocument>> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
        {
            var documents = await _storageService.UploadDocuments(request.ContainerName, request.Documents, cancellationToken);
            
            return documents;
        }
    }
}
