using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Security.KeyVault.Secrets;
using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.Application.Common.Options;
using KnowledgeMining.Application.Documents.Queries.GetDocuments;
using KnowledgeMining.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using Document = KnowledgeMining.Application.Documents.Queries.GetDocument.Document;
using SearchDocument = KnowledgeMining.Application.Documents.Queries.GetDocuments.Document;
using UploadDocument = KnowledgeMining.Application.Documents.Commands.UploadDocument.Document;
using KnowledgeMining.Application.Documents.Queries.GetDocument;
using System.ComponentModel;
using Azure.Storage.Blobs.Specialized;
using KnowledgeMining.Application.Common.Exceptions;

namespace KnowledgeMining.Infrastructure.Services.Storage
{
    public class StorageService : IStorageService
    {
        private const int MAX_ITEMS_PER_REQUEST = 5_000;
        private const int DEFAULT_PAGE_SIZE = 10;

        private readonly BlobServiceClient _blobServiceClient;
        private readonly StorageOptions _storageOptions;

        private readonly ILogger<StorageService> _logger;

        public StorageService(BlobServiceClient blobServiceClient,
                                 IOptions<StorageOptions> storageOptions,
                                 ILogger<StorageService> logger)
        {
            _blobServiceClient = blobServiceClient;
            _storageOptions = storageOptions.Value;
            _logger = logger;
        }

        public async Task<GetDocumentsResponse> GetDocuments(string container, string? searchPrefix, int pageSize, string? continuationToken, CancellationToken cancellationToken)
        {
            searchPrefix ??= string.Empty;
            pageSize = pageSize is > 0 and <= MAX_ITEMS_PER_REQUEST ? pageSize : DEFAULT_PAGE_SIZE;

            var pages = GetBlobContainerClient(container)
                            .GetBlobsAsync(traits: BlobTraits.Metadata | BlobTraits.Tags, prefix: searchPrefix, cancellationToken: cancellationToken)
                            .AsPages(continuationToken, pageSize);

            var iterator = pages.GetAsyncEnumerator(cancellationToken);

            try
            {
                await iterator.MoveNextAsync();

                var page = iterator.Current;

                return new GetDocumentsResponse()
                {
                    Documents = page?.Values?.Select(b => new SearchDocument(b.Name, b.Tags, b.Properties.LastModified, b.Metadata)) ?? Enumerable.Empty<SearchDocument>(),
                    NextPage = page?.ContinuationToken
                };
            }
            finally
            {
                await iterator.DisposeAsync();
            }
        }

        public async Task<GetDocumentsResponse> GetDocuments(string key, string container, string? searchPrefix, int pageSize, string? continuationToken, CancellationToken cancellationToken)
        {
            searchPrefix ??= string.Empty;
            pageSize = pageSize is > 0 and <= MAX_ITEMS_PER_REQUEST ? pageSize : DEFAULT_PAGE_SIZE;

            object? connectionString = null;
            if (!_storageOptions.ConnectionStrings.TryGetValue(key, out connectionString))
                _logger.LogWarning("Connection string for {Key} does not exist.", key);

            string conn = connectionString?.ToString() ?? string.Empty;

            var pages = GetBlobContainerClient(conn, container)
                            .GetBlobsAsync(traits: BlobTraits.Metadata | BlobTraits.Tags, prefix: searchPrefix, cancellationToken: cancellationToken)
                            .AsPages(continuationToken, pageSize);

            var iterator = pages.GetAsyncEnumerator(cancellationToken);

            try
            {
                await iterator.MoveNextAsync();

                var page = iterator.Current;

                return new GetDocumentsResponse()
                {
                    Documents = page?.Values?.Select(b => new SearchDocument(b.Name, b.Tags, b.Properties.LastModified, b.Metadata)) ?? Enumerable.Empty<SearchDocument>(),
                    NextPage = page?.ContinuationToken
                };
            }
            finally
            {
                await iterator.DisposeAsync();
            }
        }

        public async Task<GetDocumentResponse> GetDocument(string key, string container, string filename, CancellationToken cancellationToken) =>
            await GetDocument(key, container, filename, false, cancellationToken);

        public async Task<GetDocumentResponse> GetDocument(string key, string container, string filename, bool downloadContent, CancellationToken cancellationToken)
        {
            object? connectionString = null;
            if(!_storageOptions.ConnectionStrings.TryGetValue(key, out connectionString))
                _logger.LogWarning("Connection string for {Key} does not exist.", key);

            string conn = connectionString?.ToString() ?? string.Empty;

            if (!string.IsNullOrEmpty(conn))
            {
                var client = GetBlobContainerClient(conn, 
                    container);
                var blobClient = client.GetBlobClient(filename);

                try
                {

                    if (!downloadContent)
                    {
                        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
                        return new GetDocumentResponse(new Document(blobClient.Name, null, properties.Value.Metadata));
                    }

                    var blob = await blobClient.DownloadContentAsync(cancellationToken);

                    if(blob != null && !blob.GetRawResponse().IsError)
                    {
                        var rawContent = blob.Value.Content.ToArray();
                        var metadata = blob.Value.Details.Metadata;
                        return new GetDocumentResponse(new Document(blobClient.Name, null, metadata, rawContent));
                    }
                }
                catch (Azure.RequestFailedException e)
                {
                    _logger.LogCritical(e, "Failed to retrieve metadata properties from {Filename}", filename);
                }
            }
            else
            {
                _logger.LogWarning("Connection string for {Key} does not exist.", key);
            }            

            return new GetDocumentResponse
            {
                Document = new Document(filename, null, null)
            };
        }

        private async Task SetDocumentMetadata(BlobClient? blob, IDictionary<string, string>? metadata, CancellationToken cancellationToken)
        {
            if (blob == null || metadata == null)
                return;

            try
            {
                var response = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);
                UpdateKeyValueData(response.Value.Metadata, metadata);

                await blob.SetMetadataAsync(metadata, cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Set {DocumentName} index tags {@Metadata} failed.", blob.Name, metadata);
                throw;
            }
        }

        private async Task SetDocumentTags(BlobClient? blob, IDictionary<string, string>? tags, CancellationToken cancellationToken)
        {
            if (blob == null || tags == null)
                return;

            try
            {
                var response = await blob.GetTagsAsync(cancellationToken: cancellationToken);
                UpdateKeyValueData(response.Value.Tags, tags);

                await blob.SetTagsAsync(response.Value.Tags, cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Set {DocumentName} index tags {@Tags} failed.", blob.Name, tags);
                throw;
            }
        }

        private void UpdateKeyValueData(IDictionary<string, string> source, IDictionary<string, string> updates)
        {
            //add new tags
            var newKeys = updates.Keys.Except(source.Keys);
            foreach (var newKey in newKeys)
                source.Add(newKey, updates[newKey]);

            var existingKeys = updates.Keys.Intersect(source.Keys);
            foreach (var existingKey in existingKeys)
                source[existingKey] = updates[existingKey];
        }

        public async Task SetDocumentTraits(SearchDocument document, DocumentTraits blobTraits, CancellationToken cancellationToken)
        {
            var blob = GetBlobContainerClient()
                .GetBlobClient(document.Name);

            if(blobTraits.HasFlag(DocumentTraits.Metadata))
            {
                await SetDocumentMetadata(blob, document.Metadata, cancellationToken);
            }

            if(blobTraits.HasFlag(DocumentTraits.Tags) && document.Tags != null)
            {
                await SetDocumentTags(blob, document.Tags, cancellationToken);
            }            
        }

        public async Task<IEnumerable<SearchDocument>> UploadDocuments(string containerName,
            IEnumerable<UploadDocument> documents, CancellationToken cancellationToken)
        {
            _ = containerName ?? throw new ArgumentNullException(nameof(containerName));

            var result = new List<SearchDocument>();

            if (documents.Any())
            {
                var container = GetBlobContainerClient(containerName);

                foreach (var file in documents)
                {
                    if (file.Content.Length > 0)
                    {
                        try
                        {
                            var blob = container.GetBlobClient(file.Name);

                            var blobHttpHeader = new BlobHttpHeaders
                            {
                                ContentType = file.ContentType
                            };

                            var uploadFileResult = 
                                await blob.UploadAsync(file.Content, httpHeaders: blobHttpHeader, cancellationToken: cancellationToken).ConfigureAwait(false);

                            if (file.Tags?.Any() ?? false)
                            {
                                var nonEmptyTags = RemoveEmptyTags(file.Tags);
                                await blob.SetTagsAsync(nonEmptyTags, cancellationToken: cancellationToken);
                                await blob.SetMetadataAsync(nonEmptyTags, cancellationToken: cancellationToken);
                            }

                            result.Add(new SearchDocument
                            {
                                Name = file.Name,
                                Tags = file.Tags
                            });
                        }
                        catch(Exception ex)
                        {
                            _logger.LogCritical(ex, "Failed to upload file {FileName}", file.Name);
                        }
                        finally
                        {
                            if (!file.LeaveOpen)
                            {
                                await file.Content.DisposeAsync().ConfigureAwait(false);
                            }
                        }
                    }
                }
            }

            return result;
        }

        private IDictionary<string, string> RemoveEmptyTags(IDictionary<string, string> tags)
        {
            return tags.Where(t => !string.IsNullOrWhiteSpace(t.Key) && !string.IsNullOrWhiteSpace(t.Value)).ToDictionary(t => t.Key, t => t.Value);
        }

        public async ValueTask<byte[]> DownloadDocument(string containerName, string documentName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(documentName) || string.IsNullOrEmpty(containerName))
            {
                return Array.Empty<byte>();
            }

            var decodedFilename = WebUtility.UrlDecode(documentName); 
            _logger.LogInformation("Download blob {Container} {DocumentName}", containerName, documentName);

            var container = GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(decodedFilename);

            if (!await blob.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning("Blob {Container}/{Name} does not exist", containerName, decodedFilename);
                return Array.Empty<byte>();
            }

            using var ms = new MemoryStream();
            var downloadResult = await blob.DownloadAsync(cancellationToken);

            await downloadResult.Value.Content.CopyToAsync(ms, cancellationToken);
            downloadResult.Value.Dispose();

            return ms.ToArray();
        }

        public async ValueTask<byte[]> DownloadJobDocument(string documentName, CancellationToken cancellationToken) =>
            await DownloadDocument(_storageOptions.JobsContainerName, documentName, cancellationToken);

        public async ValueTask<byte[]> DownloadDocument(string documentName, CancellationToken cancellationToken) =>
            await DownloadDocument(_storageOptions.ContainerName, documentName, cancellationToken);

        public async ValueTask<byte[]> DownloadSource(string documentName, CancellationToken cancellationToken) =>
            await DownloadDocument(_storageOptions.SourceContainerName, documentName, cancellationToken);

        public async ValueTask DeleteDocument(string documentName, CancellationToken cancellationToken)
        {
            var blobContainer = GetBlobContainerClient();

            await blobContainer.DeleteBlobIfExistsAsync(documentName, cancellationToken: cancellationToken);
        }

        public async ValueTask<bool> DeleteDocument(string key, string container, string filename, CancellationToken cancellationToken)
        {
            object? connectionString = null;
            if (!_storageOptions.ConnectionStrings.TryGetValue(key, out connectionString))
                _logger.LogWarning("Connection string for {Key} does not exist.", key);

            string conn = connectionString?.ToString() ?? string.Empty;

            if (!string.IsNullOrEmpty(conn))
            {
                var blobContainer = GetBlobContainerClient(conn,
                    container);
                try
                {
                    await blobContainer.DeleteBlobIfExistsAsync(filename, DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
                }
                catch (Azure.RequestFailedException e)
                {
                    _logger.LogCritical(e, "Failed to mark {Filename} for deletion.", filename);
                    return false;
                }
            }
            else
            {
                _logger.LogWarning("Connection string for {Key} does not exist.", key);
                return false;
            }
            return true;
        }

        private BlobContainerClient GetBlobContainerClient()
        {
            return GetBlobContainerClient(_storageOptions.ContainerName);
        }

        private BlobContainerClient GetBlobContainerClient(string containerName)
        {
            return _blobServiceClient.GetBlobContainerClient(containerName);
        }

        private BlobContainerClient GetBlobContainerClient(string connectionString, string container)
        {
            var svc = new BlobServiceClient(connectionString);
            return svc.GetBlobContainerClient(container);

        }

        public async Task RenameDocument(string key, string container, string currentName, string newName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(container) || string.IsNullOrEmpty(currentName) || string.IsNullOrEmpty(newName))
            {
                _logger.LogWarning("One or more arguments were not provided.");
                throw new ArgumentException("One or more required arguments are null or empty.");
            }

            // Renaming a blob involves the copy operations. Reusing the move document method with the source and destination container names being the same.
            await MoveDocument(key, container, currentName, container, newName, cancellationToken);
        }

        public async Task MoveDocument(string key, string srcContainer, string srcName, string destContainer, string destName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(srcContainer) || string.IsNullOrEmpty(srcName) || string.IsNullOrEmpty(destContainer) || string.IsNullOrEmpty(destName))
            {
                _logger.LogWarning("One or more arguments were not provided.");
                throw new ArgumentException("One or more arguments were not provided.");
            }

            object? connectionString = null;
            if (!_storageOptions.ConnectionStrings.TryGetValue(key, out connectionString))
            {
                _logger.LogWarning("Connection string for {Key} does not exist.", key);
                throw new ArgumentException("Connection string for " + key + " does not exist.");
            }
                
            string conn = connectionString?.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(conn))
            {
                throw new StorageServiceOperationException("Connection string cannot be null or empty.");
            }

            // Set source and destination blobs
            var sourceContainer = GetBlobContainerClient(conn, srcContainer);
            var srcBlob = sourceContainer.GetBlobClient(srcName);

            var destinationContainer = GetBlobContainerClient(conn, destContainer);
            var destBlob = destinationContainer.GetBlobClient(destName);

            // Copy blob with new name
            try
            {
                var copyOperation = await destBlob.StartCopyFromUriAsync(new Uri(srcBlob.Uri.ToString()), cancellationToken: cancellationToken);
            }
            catch
            {
                throw new StorageServiceOperationException("Move Operation Failed: Blob " + srcName + " could not be moved.");
            }
            
            try
            {
                var response = await srcBlob.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
            }
            catch
            {
                throw new StorageServiceOperationException("Move Operation Failed: Blob " + srcBlob + " could not be moved.");
            }
        }
    }
}
