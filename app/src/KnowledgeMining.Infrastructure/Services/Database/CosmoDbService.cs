using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.Application.Common.Options;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeMining.Infrastructure.Services.Database
{
    public class CosmoDbService : IDatabaseService
    {
        private readonly DatabaseOptions _options;
        private readonly Container _container;

        private readonly ILogger _logger;

        public CosmoDbService(CosmosClient dbClient,
                                 IOptions<DatabaseOptions> options,
                                 ILogger<CosmoDbService> logger)
        {
            _options = options.Value;
            _container = dbClient.GetContainer(_options.DatabaseName, _options.ContainerName);
            _logger = logger;
        }
    }
}
