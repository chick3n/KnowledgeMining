using KnowledgeMining.Application.Common.Interfaces;
using RequestDocument = KnowledgeMining.Application.Documents.Commands.UploadDocument.Document;
using MediatR;
using KnowledgeMining.Domain.Entities.Messages;
using KnowledgeMining.Domain.Entities;
using System.Text.Json;
using System.Text;
using KnowledgeMining.Domain.Entities.Jobs;

namespace KnowledgeMining.Application.Documents.Commands.DocumentRequest
{
    public readonly record struct NewDocumentRequestCommand(DocumentJobRequest Job) : 
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
            var job = request.Job;
            var payload = JsonSerializer.Serialize(new DocumentJobRequestMessage(DateTimeOffset.UtcNow.AddDays(7),
                job.Id,
                job.IndexConfig,
                job.Action));

            var dbSuccess = await _databaseService.CreateDocumentJob(job, cancellationToken);
            if (dbSuccess)
            {
                var receipt = await _queueService.SendDocumentJobRequest(payload);
                return receipt;
            }
            return new QueueReceipt(null);
        }
    }
}
