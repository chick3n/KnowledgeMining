using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.Application.Common.Options;
using KnowledgeMining.Infrastructure.Services.Database;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeMining.Infrastructure.Extensions
{
    public static class DatabaseClientBuilderExtension
    {
        public static IServiceCollection AddDatabaseClient(this IServiceCollection services, IConfigurationSection options)
        {
            var databaseOptions = options.Get<DatabaseOptions>();
            CosmosClient client = new CosmosClient(databaseOptions.ConnectionString);
            services.AddSingleton(client);
            services.AddTransient<IDatabaseService, CosmoDbService>();
            return services;
        }
    }
}
