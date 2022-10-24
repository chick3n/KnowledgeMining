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
using KnowledgeMining.Domain.Enums;
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

        private async Task<QueueReceipt> SendExtractiveSummaryRequest(string message, CancellationToken cancellationToken = default)
        {
            _ = _queueOptions.ExtractiveSummaryRequests ?? throw new ArgumentNullException(nameof(_queueOptions.ExtractiveSummaryRequests));

            var response = await SendMessage(_queueOptions.ExtractiveSummaryRequests,
                message,
                cancellationToken: cancellationToken);

            if (response == null)
                throw new ArgumentNullException(nameof(QueueReceipt));
            return new QueueReceipt(response.MessageId, response.InsertionTime, response.ExpirationTime, response.PopReceipt, response.TimeNextVisible);
        }

        private async Task<QueueReceipt> SendAbstractiveSummaryRequest(string message, CancellationToken cancellationToken = default)
        {
            _ = _queueOptions.AbstractiveSummaryRequests ?? throw new ArgumentNullException(nameof(_queueOptions.AbstractiveSummaryRequests));

            var response = await SendMessage(_queueOptions.AbstractiveSummaryRequests,
                message,
                cancellationToken: cancellationToken);

            if (response == null)
                throw new ArgumentNullException(nameof(QueueReceipt));
            return new QueueReceipt(response.MessageId, response.InsertionTime, response.ExpirationTime, response.PopReceipt, response.TimeNextVisible);
        }

        private QueueClient GetQueueClient(string queueName)
        {
            return _queueServiceClient.GetQueueClient(queueName);
        }

        private async Task<SendReceipt?> SendMessage(string queueName, string message, bool createQueueIfMissing=false, CancellationToken cancellationToken = default)
        {
            var client = GetQueueClient(queueName);

            await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            return await GetQueueClient(queueName)
                .SendMessageAsync(message, cancellationToken);
        }

        public async Task<QueueReceipt> SendJobRequest(ServiceType serviceType, string message, CancellationToken cancellationToken)
        {
            var queueName = GetQueueName(serviceType);

            var response = await SendMessage(queueName,
                message,
                createQueueIfMissing: true,
                cancellationToken: cancellationToken);

            if (response == null)
                throw new ArgumentNullException(nameof(QueueReceipt));
            return new QueueReceipt(response.MessageId, response.InsertionTime, response.ExpirationTime, response.PopReceipt, response.TimeNextVisible);
        }

        private string GetQueueName(ServiceType serviceType)
        {
            string? queueName = null;
            switch(serviceType)
            {
                case ServiceType.AbstractiveSummary:
                    queueName = _queueOptions.AbstractiveSummaryRequests;
                    break;
                case ServiceType.ExtractiveSummary:
                    queueName = _queueOptions.ExtractiveSummaryRequests;
                    break;
                case ServiceType.Prompt:
                    queueName = _queueOptions.PromptRequests;
                    break;
            }

            if (string.IsNullOrEmpty(queueName))
            {
                throw new ArgumentNullException($"Queue name for {serviceType.ToString()} not found.");
            }

            return queueName;
        }
    }
}
