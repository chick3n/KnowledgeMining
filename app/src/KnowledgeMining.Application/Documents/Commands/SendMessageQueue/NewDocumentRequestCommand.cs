using KnowledgeMining.Application.Common.Interfaces;
using RequestDocument = KnowledgeMining.Application.Documents.Commands.UploadDocument.Document;
using MediatR;
using KnowledgeMining.Domain.Entities.Messages;
using KnowledgeMining.Domain.Entities;
using System.Text.Json;
using System.Text;

namespace KnowledgeMining.Application.Documents.Commands.SendMessageToQueue
{
    public readonly record struct NewDocumentRequestCommand(DocumentRequestMessage Message) : 
        IRequest<QueueReceipt>;

    public class NewDocumentRequestCommandHandler : IRequestHandler<NewDocumentRequestCommand, QueueReceipt>
    {
        private readonly IStorageService _storageService;
        private readonly IQueueService _queueService;
        private readonly IDatabaseService _databaseService;

        public NewDocumentRequestCommandHandler(IStorageService storageService,
            IQueueService queueService,
            IDatabaseService databaseService)
        {
            _storageService = storageService;
            _queueService = queueService;
            _databaseService = databaseService;
        }

        public async Task<QueueReceipt> Handle(NewDocumentRequestCommand request, CancellationToken cancellationToken)
        {
            var message = request.Message.DocumentRequest;
            var payload = JsonSerializer.Serialize(message);

            using var mem = new MemoryStream(Encoding.UTF8.GetBytes(payload));
            var uploadDocument = new RequestDocument(message.Id, "application/json", null, mem);
            await _storageService.UploadDocuments("jobs", new RequestDocument[] { uploadDocument }, cancellationToken);

            await _databaseService.CreateDocumentJob(message, cancellationToken);            
            var receipt = await _queueService.SendDocumentRequest(payload);
            return receipt;
        }
    }
}
