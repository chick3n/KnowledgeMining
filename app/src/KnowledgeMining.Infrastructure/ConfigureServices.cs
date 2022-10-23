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
using KnowledgeMining.Infrastructure.Services.Queue;
using Azure.Storage.Queues;
using Azure;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, ConfigurationManager configuration)
        {
            //services.Configure<KMOptions.SearchOptions>(configuration.GetSection(KMOptions.SearchOptions.Search));

            services.AddAzureClients(clientBuilder =>
            {
                /*clientBuilder.ConfigureDefaults(configuration.GetSection("AzureDefaults"));
                clientBuilder.UseCredential(new DefaultAzureCredential());
                clientBuilder.AddBlobServiceClient(configuration.GetSection(KMOptions.StorageOptions.Storage));

                //clientBuilder.AddSearchClient(configuration.GetSection(KMOptions.SearchOptions.Search));
                clientBuilder.AddSearchIndexClient(configuration.GetSection(KMOptions.SearchOptions.Search));
                //clientBuilder.AddSearchIndexerClient(configuration.GetSection(KMOptions.SearchOptions.Search));
                clientBuilder.AddQueueServiceClient(configuration.GetSection(KMOptions.QueueOptions.Queue))
                    .ConfigureOptions(opt =>
                    {
                        opt.MessageEncoding = QueueMessageEncoding.Base64;
                    });*/

                clientBuilder.ConfigureDefaults(configuration.GetSection("AzureDefaults"));
                clientBuilder.AddBlobServiceClient(configuration.GetValue<string>("Storage:ConnectionString"));

                clientBuilder.AddSearchIndexClient(new Uri(configuration.GetValue<string>("Search:Endpoint")), 
                    new AzureKeyCredential(configuration.GetValue<string>("Search:Credential:Key")));
                clientBuilder.AddQueueServiceClient(configuration.GetValue<string>("Queue:ConnectionString"))
                    .ConfigureOptions(opt =>
                    {
                        opt.MessageEncoding = QueueMessageEncoding.Base64;
                    });

                //Extension has issue grabing connectionstring from config, doing manually now
                var connString = configuration.GetSection("Database:ConnectionString").Get<string>();
                clientBuilder.AddTableServiceClient(connString);
            });

            services.AddHttpClient(KMOptions.SearchOptions.Search, client =>
            {
                var endpoint = configuration
                    .GetSection(KMOptions.SearchOptions.Search).GetValue<string>(nameof(KMOptions.SearchOptions.Endpoint));
                var apiKey = configuration
                    .GetSection(KMOptions.SearchOptions.Search)
                    .GetValue<string>("Credential:Key");
                client.BaseAddress = new Uri(endpoint, UriKind.Absolute);
                //client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                client.DefaultRequestHeaders.Add("api-key", apiKey);
            });

            services.AddScoped<ISearchService, SearchService>();
            services.AddScoped<IStorageService, StorageService>();
            services.AddScoped<IDatabaseService, DatabaseService>();
            services.AddScoped<IQueueService, QueueService>();


            services.AddSingleton(Channel.CreateUnbounded<SearchIndexerJobContext>(new UnboundedChannelOptions() { SingleWriter = true, SingleReader = true }));
            services.AddSingleton(provider =>
            {
                return provider.GetService<Channel<SearchIndexerJobContext>>()!.Writer;
            });
            services.AddSingleton(provider =>
            {
                return provider.GetService<Channel<SearchIndexerJobContext>>()!.Reader;
            });
            //services.AddHostedService<SearchIndexerJob>();



            return services;
        }
    }
}
