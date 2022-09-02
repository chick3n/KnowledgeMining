using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.Application.Common.Options;
using KnowledgeMining.Domain.Entities;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeMining.Infrastructure.Services.Database
{
    public class CosmoDbService : IDatabaseService
    {
        private readonly DatabaseOptions _options;
        private readonly Container _indexContainer;
        private readonly Container _appContainer;

        private readonly ILogger _logger;

        public CosmoDbService(CosmosClient dbClient,
                                 IOptions<DatabaseOptions> options,
                                 ILogger<CosmoDbService> logger)
        {
            _options = options.Value;
            _appContainer = dbClient.GetContainer(_options.DatabaseName, _options.AppContainer);
            _indexContainer = dbClient.GetContainer(_options.DatabaseName, _options.IndexContainer);
            _logger = logger;
        }

        public async Task<IEnumerable<IndexItem>> GetIndices()
        {
            var query = _indexContainer.GetItemQueryIterator<IndexItem>(new QueryDefinition("SELECT * FROM indices"));
            List<IndexItem> results = new List<IndexItem>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();

                results.AddRange(response.ToList());
            }

            return results;
        }
    }
}
