using KnowledgeMining.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeMining.Application.Common.Interfaces
{
    public interface IQueueService
    {
        Task<QueueReceipt> SendExtractiveSummaryRequest(string message, CancellationToken cancellationToken = default);
        Task<QueueReceipt> SendAbstractiveSummaryRequest(string message, CancellationToken cancellationToken = default);
    }
}
