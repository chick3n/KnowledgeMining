using Azure.Storage.Blobs;
using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.Application.Common.Options;
using KnowledgeMining.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KnowledgeMining.Infrastructure.Services.Database
{
    public class DatabaseService : IDatabaseService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly DatabaseOptions _options;
        private readonly ILogger<DatabaseService> _logger;
        private readonly IMemoryCache _cache;

        public DatabaseService(BlobServiceClient blobServiceClient,
                                IMemoryCache memoryCache,
                                 IOptions<DatabaseOptions> options,
                                 ILogger<DatabaseService> logger)
        {
            _blobServiceClient = blobServiceClient;
            _options = options.Value;
            _cache = memoryCache;
            _logger = logger;
        }

        private void SetCachedItem<T>(string key, T item)
        {
            if(item != null)
                _cache.Set(key, item, TimeSpan.FromMinutes(10)); 
        }

        public async Task<IndexItem> GetIndex(string indexName, CancellationToken cancellationToken)
        {
            _ = indexName ?? throw new ArgumentNullException(nameof(indexName));

            var fileName = $"{indexName.ToLower()}.json";

            _cache.TryGetValue<IndexItem>(fileName, out var index);

            if (index == null)
            {
                var client = _blobServiceClient.GetBlobContainerClient(_options.IndexContainer);
                var blob = client.GetBlobClient(fileName);

                var exists = await blob.ExistsAsync(cancellationToken);
                if (!exists) throw new ArgumentOutOfRangeException($"Index {fileName} does not exist");

                var blobFile = await blob.DownloadContentAsync(cancellationToken);
                index = blobFile.Value.Content.ToObjectFromJson<IndexItem>();
                if (index == null) throw new JsonException($"Index {fileName} cannot be deserialized");
                SetCachedItem(fileName, index);
            }

            return index;
        }

        public Task<IEnumerable<IndexItem>> GetIndices(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<Metrics> GetMetrics(string metricsName, CancellationToken cancellationToken)
        {
            _ = metricsName ?? throw new ArgumentNullException(nameof(metricsName));

            var fileName = $"{metricsName.ToLower()}.json";
            var cache_key = $"metrics_{fileName}";
            

            _cache.TryGetValue<Metrics>(cache_key, out var metrics);

            if (metrics == null)
            {
                var client = _blobServiceClient.GetBlobContainerClient(_options.MetricsContainer);
                var blob = client.GetBlobClient(fileName);

                var exists = await blob.ExistsAsync(cancellationToken);
                if (!exists) throw new ArgumentOutOfRangeException($"Metrics {fileName} does not exist");

                var blobFile = await blob.DownloadContentAsync(cancellationToken);
                metrics = blobFile.Value.Content.ToObjectFromJson<Metrics>();
                if (metrics == null) throw new JsonException($"Metrics {fileName} cannot be deserialized");
                SetCachedItem(cache_key, metrics);
            }

            return metrics;
        }
    }
}
