﻿using Azure.Data.Tables;
using Azure.Storage.Blobs;
using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.Application.Common.Mappings;
using KnowledgeMining.Application.Common.Options;
using KnowledgeMining.Domain.Entities;
using KnowledgeMining.Domain.Entities.Jobs;
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
        private readonly TableServiceClient _tableServiceClient;

        private const string TABLE_JOBS = "jobs";
        private const string TABLE_JOBDOCUMENTS = "jobDocuments";
        private const string TABLE_JOBOPTIONS = "jobOptions";

        public DatabaseService(BlobServiceClient blobServiceClient,
                                TableServiceClient tableServiceClient,
                                IMemoryCache memoryCache,
                                 IOptions<DatabaseOptions> options,
                                 ILogger<DatabaseService> logger)
        {
            _blobServiceClient = blobServiceClient;
            _tableServiceClient = tableServiceClient;
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

        public async Task<bool> CreateDocumentJob(DocumentJobRequest documentRequest, CancellationToken cancellationToken = default)
        {
            var entity = new Models.Job
            {
                PartitionKey = documentRequest.Index,
                RowKey = documentRequest.Id,
                Timestamp = DateTimeOffset.UtcNow,
                CreatedBy = documentRequest.CreatedBy,
                CreatedOn = DateTimeOffset.Now,
                State = documentRequest.State,
                Action = documentRequest.Action.ToString(),
                ETag = new Azure.ETag("1")
            };

            var response = await _tableServiceClient.GetTableClient(TABLE_JOBS)
                .AddEntityAsync(entity, cancellationToken);
            if (response.IsError)
            {
                //Todo capture logging the issue
                return false;
            }

            var documentResponse = await CreateDocumentJobFiles(documentRequest, cancellationToken);
            if (!documentResponse)
                return false;

            var optionResponse = await CreateDocumentJobOptions(documentRequest, cancellationToken);
            if (!optionResponse)
                return false;

            return true;
        }

        private async Task<bool> CreateDocumentJobFiles(DocumentJobRequest documentRequest, CancellationToken cancellationToken)
        {
            var documentTableClient = _tableServiceClient.GetTableClient(TABLE_JOBDOCUMENTS);
            await documentTableClient.CreateIfNotExistsAsync(cancellationToken);

            foreach (var documentItem in documentRequest.Documents)
            {
                var documentEntity =
                    new Models.JobDocument { PartitionKey = documentRequest.Id, RowKey = documentItem.Id, Title = documentItem.Title };
                var response = await documentTableClient.AddEntityAsync(documentEntity, cancellationToken);

                if (response.IsError)
                    return false; //Todo capture logging and clean up rows
            }

            return true;
        }

        private async Task<bool> CreateDocumentJobOptions(DocumentJobRequest documentRequest, CancellationToken cancellationToken)
        {
            var optionTableClient = _tableServiceClient.GetTableClient(TABLE_JOBOPTIONS);
            await optionTableClient.CreateIfNotExistsAsync(cancellationToken);

            foreach (var option in documentRequest.Options)
            {
                var optionEntity = new Models.JobOption
                {
                    PartitionKey = documentRequest.Id,
                    RowKey = Guid.NewGuid().ToString("D"),
                    Name = option.Key,
                    Value = option.Value
                };

                var response = await optionTableClient.AddEntityAsync(optionEntity, cancellationToken);
                if (response.IsError)
                    return false;
            }

            return true;
        }

        public async Task<IList<DocumentJobRequest>> GetDocumentJobs(string indexName, CancellationToken cancellationToken = default)
        {
            var jobs = new List<DocumentJobRequest>();
            var results = _tableServiceClient.GetTableClient(TABLE_JOBS)
                    .QueryAsync<Models.Job>(x => x.PartitionKey.Equals(indexName), maxPerPage: 100,
                        cancellationToken: cancellationToken);

            await foreach(var page in results.AsPages())
            {
                foreach (var entity in page.Values) {
                    jobs.Add(new DocumentJobRequest
                    {
                        Action = ServiceTypeMapper.FromString(entity.Action),
                        CreatedBy = entity.CreatedBy,
                        CreatedOn = entity.CreatedOn.Ticks,
                        CreatedOnOffset = entity.CreatedOn.Offset.Ticks,
                        Id = entity.RowKey,
                        Index = entity.PartitionKey,
                        State = entity.State
                    });
                }
            }

            return jobs;
        }

        public async Task<DocumentJobRequest> GetDocumentJob(string indexName, string id, CancellationToken cancellationToken = default)
        {
            var result = await _tableServiceClient.GetTableClient(TABLE_JOBS)
                    .GetEntityAsync<Models.Job>(indexName, id, cancellationToken: cancellationToken);

            if (result == null || result.Value == null)
                throw new FileNotFoundException($"Job {indexName}/{id} cannot be found.");

            var documentJobRequest = new DocumentJobRequest
            {
                Action = ServiceTypeMapper.FromString(result.Value.Action),
                CreatedBy = result.Value.CreatedBy,
                CreatedOn = result.Value.CreatedOn.Ticks,
                CreatedOnOffset = result.Value.CreatedOn.Offset.Ticks,
                Id = result.Value.RowKey,
                Index = result.Value.PartitionKey,
                State = result.Value.State
            };

            var documents = new List<DocumentItem>();

            var documentsResult = _tableServiceClient.GetTableClient(TABLE_JOBDOCUMENTS)
                .QueryAsync<Models.JobDocument>(x => x.PartitionKey.Equals(id), maxPerPage: 100, cancellationToken: cancellationToken);

            await foreach (var page in documentsResult.AsPages())
            {
                foreach (var entity in page.Values)
                {
                    documents.Add(new DocumentItem
                    {
                        Id = entity.RowKey,
                        Title = entity.Title
                    });
                }
            }

            documentJobRequest.Documents = documents;

            var options = new Dictionary<string, string>();
            var optionsResult = _tableServiceClient.GetTableClient(TABLE_JOBOPTIONS)
                .QueryAsync<Models.JobOption>(x => x.PartitionKey.Equals(id), maxPerPage: 100, cancellationToken: cancellationToken);

            await foreach(var page in optionsResult.AsPages())
            {
                foreach(var entity in page.Values)
                {
                    options.TryAdd(entity.Name, entity.Value);
                }
            }

            documentJobRequest.Options = options;

            return documentJobRequest;
        }
    }
}
