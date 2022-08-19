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
        private readonly ILogger _logger;
        private readonly DocumentCacheService _documentCachingService;


        public DocumentFilterScopedService(ILogger<DocumentFilterScopedService> logger,
            DocumentCacheService documentCachingService)
        {
            _logger = logger;
            _documentCachingService = documentCachingService;
        }

        public async Task DoWork(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                executionCount++;

                _logger.LogInformation(
                    "Scoped Processing Service is working. Count: {Count}", executionCount);

                await _documentCachingService.BuildCache(stoppingToken);

                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }
}