using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.Application.Documents.Queries.GetDocuments;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace KnowledgeMining.UI.Services.Documents
{
    internal interface IScopedProcessingService
    {
        Task DoWork(CancellationToken stoppingToken);
    }

    public class DocumentFilterScopedService : IScopedProcessingService
    {
        private int executionCount = 0;
        private int pageSize = 100;
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly IMemoryCache _memoryCache;


        public DocumentFilterScopedService(ILogger<DocumentFilterScopedService> logger,
            IMediator mediator,
            IMemoryCache memoryCache)
        {
            _logger = logger;
            _mediator = mediator;
            _memoryCache = memoryCache;
        }

        public async Task DoWork(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                executionCount++;

                _logger.LogInformation(
                    "Scoped Processing Service is working. Count: {Count}", executionCount);

                List<Document> documents = new List<Document>();
                string? nextPage = default;
                do
                {
                    var response = await _mediator.Send(new GetDocumentsQuery(null, pageSize, nextPage));
                    nextPage = response.NextPage;
                    documents.AddRange(response.Documents);
                } while (!string.IsNullOrWhiteSpace(nextPage));

                _logger.LogInformation("Processed {Count} storage blobs", documents.Count);
                _memoryCache.Set(Constants.DOCUMENT_FILTER_CACHE, documents);

                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }
}