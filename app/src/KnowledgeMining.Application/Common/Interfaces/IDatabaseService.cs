using KnowledgeMining.Domain.Entities;
using KnowledgeMining.Domain.Entities.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeMining.Application.Common.Interfaces
{
    public interface IDatabaseService
    {
        public Task<IEnumerable<IndexItem>> GetIndices(CancellationToken cancellationToken);
        public Task<IndexItem> GetIndex(string indexName, CancellationToken cancellationToken);
        public Task<Metrics> GetMetrics(string metricsName, CancellationToken cancellationToken);
        public Task CreateDocumentJob(DocumentRequest documentRequest, CancellationToken cancellationToken);
    }
}
