using KnowledgeMining.Application.Documents.Queries.GetDocuments;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace KnowledgeMining.UI.Services.Documents
{
    public class DocumentCacheService
    {
        private readonly IMediator _mediator;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger _logger;

        private readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);
        private const int PAGESIZE = 100;
        public const string DOCUMENT_FILTER_CACHE = "DOCUMENTFILTERCACHE";

        public DocumentCacheService(IMediator mediator, IMemoryCache memoryCache, ILogger<DocumentCacheService> logger)
        {
            _mediator = mediator;
            _memoryCache = memoryCache;
            _logger = logger; 
        }

        public async Task<IEnumerable<Document>> BuildCache(CancellationToken cancellationToken)
        {
            await _locker.WaitAsync(cancellationToken);
            List<Document> documents = new List<Document>();

            if (_memoryCache.TryGetValue(DOCUMENT_FILTER_CACHE, out _))
                _memoryCache.Remove(DOCUMENT_FILTER_CACHE);

            try
            {                
                string? nextPage = default;
                do
                {
                    var response = await _mediator.Send(new GetDocumentsQuery(null, PAGESIZE, nextPage));
                    nextPage = response.NextPage;
                    documents.AddRange(response.Documents);
                } while (!string.IsNullOrWhiteSpace(nextPage));

                _logger.LogInformation("Processed {Count} storage blobs", documents.Count);
                _memoryCache.Set(DOCUMENT_FILTER_CACHE, documents);

            }
            catch(Exception ex)
            {
                _logger.LogCritical("Build cached unknown failure.", ex);
            }
            finally
            {
                _locker.Release();
            }

            return documents;
        }

        public async Task UpdateDocument(Document updatedDocument)
        {
            if (!_memoryCache.TryGetValue<IList<Document>>(DOCUMENT_FILTER_CACHE, out var documents))
                return;

            for(var x=0; x<documents.Count; x++)
            {
                var document = documents[x];
                if(document.Name.Equals(updatedDocument.Name))
                {
                    documents[x] = updatedDocument;

                    await _locker.WaitAsync();

                    _memoryCache.Set(DOCUMENT_FILTER_CACHE, documents);

                    _locker.Release();

                    break;
                }                
            }
        }

        public async Task SetDocumnets(IEnumerable<Document> documents)
        {
            await _locker.WaitAsync();

            _memoryCache.Set(DOCUMENT_FILTER_CACHE, documents);

            _locker.Release();

        }

        public IEnumerable<Document> GetDocuments()
        {
            _memoryCache.TryGetValue<IEnumerable<Document>>(DOCUMENT_FILTER_CACHE, out var documents);
            if (documents == null)
                documents = new List<Document>();

            return documents;
        }

        public async Task AddDocuments(IEnumerable<Document> documents)
        {
            _memoryCache.TryGetValue<List<Document>>(DOCUMENT_FILTER_CACHE, out var cachedDocuments);
            if (cachedDocuments == null)
                cachedDocuments = new List<Document>();

            cachedDocuments.AddRange(documents);
            await SetDocumnets(cachedDocuments);
        }

        public IEnumerable<Document> RemoveDocuments(Func<Document, bool> predicate)
        {
            var documents = GetDocuments()
                .Where(predicate);

            _memoryCache.Set(DOCUMENT_FILTER_CACHE, documents);
            return documents;
        }
    }
}
