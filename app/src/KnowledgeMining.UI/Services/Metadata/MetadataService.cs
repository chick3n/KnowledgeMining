using KnowledgeMining.Infrastructure.Services.Storage;
using KnowledgeMining.UI.Services.Documents;
using Microsoft.Extensions.Caching.Memory;

namespace KnowledgeMining.UI.Services.Metadata
{
    public class MetadataService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger _logger;
        private readonly DocumentCacheService _documentCacheService;

        public const string MISSION_FILTER_CACHE = "MISSIONFILTERCACHE";
        public const string DOCUMENTTYPE_FILTER_CACHE = "DOCUMENTTYPEFILTERCACHE";

        public MetadataService(IMemoryCache memoryCache, 
            ILogger<MetadataService> logger,
            DocumentCacheService documentCacheService)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _documentCacheService = documentCacheService;
        }
        public async Task<IEnumerable<string>> DocumentTypes()
        {
            return await TryGet(DOCUMENTTYPE_FILTER_CACHE, BlobMetadata.DocumentType);
        }

        public async Task<IEnumerable<string>> Missions()
        {
            return await TryGet(MISSION_FILTER_CACHE, BlobMetadata.Mission);
        }

        private async Task<IEnumerable<string>> TryGet(string key, string metadataKey)
        {
            if(!_memoryCache.TryGetValue<ISet<string>>(key, out var uniqueSet))
            {
                uniqueSet = new SortedSet<string>();
                var documents = await _documentCacheService.BuildCache(new CancellationToken());

                foreach(var document in documents)
                {
                    if (document.Metadata != null
                        && document.Metadata.ContainsKey(metadataKey)
                        && !string.IsNullOrWhiteSpace(document.Metadata[metadataKey]))
                        uniqueSet.Add(document.Metadata[metadataKey]);
                }

                _memoryCache.Set(key, uniqueSet);
            }

            return uniqueSet;
        }


    }
}
