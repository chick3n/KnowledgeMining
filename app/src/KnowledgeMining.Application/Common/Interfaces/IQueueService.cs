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
        Task<QueueReceipt> SendDocumentRequest(string message, CancellationToken cancellationToken = default);
    }
}
