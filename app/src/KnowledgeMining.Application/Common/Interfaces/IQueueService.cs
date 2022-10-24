using KnowledgeMining.Domain.Entities;
using KnowledgeMining.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeMining.Application.Common.Interfaces
{
    public interface IQueueService
    {
        Task<QueueReceipt> SendJobRequest(ServiceType serviceType, string message, CancellationToken cancellationToken);
    }
}
