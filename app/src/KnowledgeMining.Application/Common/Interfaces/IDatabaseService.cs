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
        Task<IEnumerable<IndexItem>> GetIndices(CancellationToken cancellationToken);
        Task<IndexItem> GetIndex(string indexName, CancellationToken cancellationToken);
        Task<Metrics> GetMetrics(string metricsName, CancellationToken cancellationToken);
        Task<bool> CreateDocumentJob(DocumentJobRequest documentRequest, CancellationToken cancellationToken);
        Task<IList<DocumentJobRequest>> GetDocumentJobs(string indexName, CancellationToken cancellationToken);
        Task<DocumentJobRequest> GetDocumentJob(string indexName, string id, CancellationToken cancellationToken);
    }
}
