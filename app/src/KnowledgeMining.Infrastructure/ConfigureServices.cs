using Azure.Identity;
using Azure.Search.Documents;
using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.Application.Documents.Commands.DeleteDocument;
using KnowledgeMining.Infrastructure.Extensions;
using KnowledgeMining.Infrastructure.Jobs;
using KnowledgeMining.Infrastructure.Services.Database;
using KnowledgeMining.Infrastructure.Services.Search;
using KnowledgeMining.Infrastructure.Services.Storage;
using KMOptions = KnowledgeMining.Application.Common.Options;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, ConfigurationManager configuration)
        {
            //services.Configure<KMOptions.SearchOptions>(configuration.GetSection(KMOptions.SearchOptions.Search));

            services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.ConfigureDefaults(configuration.GetSection("AzureDefaults"));
                clientBuilder.UseCredential(new DefaultAzureCredential());

                clientBuilder.AddBlobServiceClient(configuration.GetSection(KMOptions.StorageOptions.Storage));

                //clientBuilder.AddSearchClient(configuration.GetSection(KMOptions.SearchOptions.Search));
                clientBuilder.AddSearchIndexClient(configuration.GetSection(KMOptions.SearchOptions.Search));
                clientBuilder.AddSearchIndexerClient(configuration.GetSection(KMOptions.SearchOptions.Search));
            });

            services.AddScoped<ISearchService, SearchService>();
            services.AddScoped<IStorageService, StorageService>();
            services.AddScoped<IDatabaseService, DatabaseService>();


            services.AddSingleton(Channel.CreateUnbounded<SearchIndexerJobContext>(new UnboundedChannelOptions() { SingleWriter = true, SingleReader = true }));
            services.AddSingleton(provider =>
            {
                return provider.GetService<Channel<SearchIndexerJobContext>>()!.Writer;
            });
            services.AddSingleton(provider =>
            {
                return provider.GetService<Channel<SearchIndexerJobContext>>()!.Reader;
            });
            services.AddHostedService<SearchIndexerJob>();



            return services;
        }
    }
}
