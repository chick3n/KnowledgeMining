using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.Application.Common.Options;
using KnowledgeMining.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeMining.Infrastructure.Services.Queue
{
    public class QueueService : IQueueService
    {
        private readonly QueueServiceClient _queueServiceClient;
        private readonly ILogger _logger;
        private readonly QueueOptions _queueOptions;

        public QueueService(QueueServiceClient queueServiceClient,
            IOptions<QueueOptions> queueOptions,
            ILogger<QueueService> logger)
        {
            _queueServiceClient = queueServiceClient;
            _logger = logger;
            _queueOptions = queueOptions.Value;
        }

        public async Task<QueueReceipt> SendDocumentRequest(string message, CancellationToken cancellationToken = default)
        {
            _ = _queueOptions.DocumentRequests ?? throw new ArgumentNullException(nameof(_queueOptions.DocumentRequests));

            var response = await SendMessage(_queueOptions.DocumentRequests,
                message);

            if (response == null)
                throw new ArgumentNullException(nameof(QueueReceipt));
            return new QueueReceipt(response.MessageId, response.InsertionTime, response.ExpirationTime, response.PopReceipt, response.TimeNextVisible);
        }

        private QueueClient GetQueueClient(string queueName)
        {
            return _queueServiceClient.GetQueueClient(queueName);
        }

        private async Task<SendReceipt?> SendMessage(string queueName, string message, CancellationToken cancellationToken = default)
        {
            return await GetQueueClient(queueName)
                .SendMessageAsync(message, cancellationToken);
        }
    }
}
