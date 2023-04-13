using Blazored.LocalStorage;
using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.UI.Api;
using KnowledgeMining.UI.Extensions;
using KnowledgeMining.UI.Services.Documents;
using KnowledgeMining.UI.Services.Links;
using KnowledgeMining.UI.Services.Metadata;
using KnowledgeMining.UI.Services.State;
using MediatR;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;
using System.Text;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace KnowledgeMining.UI
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.CaptureStartupErrors(true);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Add localization services ----CHECK HERE
            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
            builder.Services.AddControllers();

            // Add services to the container.
            builder.Services.AddApplicationInsightsTelemetry();
            builder.Services.AddMemoryCache();
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddMudServices(config =>
            {
                config.SnackbarConfiguration.PreventDuplicates = false;
                config.SnackbarConfiguration.NewestOnTop = true;
                config.SnackbarConfiguration.ShowCloseIcon = true;
                config.SnackbarConfiguration.VisibleStateDuration = 10000;
                config.SnackbarConfiguration.HideTransitionDuration = 500;
                config.SnackbarConfiguration.ShowTransitionDuration = 500;
                config.SnackbarConfiguration.SnackbarVariant = MudBlazor.Variant.Filled;
            });
            builder.Services.AddHttpClient(PreviewFileEndpoint.EndpointName);
            builder.Services.AddBlazoredLocalStorage();

            builder.Services.AddSignalR().AddAzureSignalR(options =>
            {
                options.ServerStickyMode = Microsoft.Azure.SignalR.ServerStickyMode.Required;
            });

            builder.Services.AddResponseCompression(options =>
            {
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });

            builder.Services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            builder.Services.Configure<HostOptions>(opt =>
            {
                opt.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
            });

            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("en"),
                    new CultureInfo("fr"),
                };
                options.DefaultRequestCulture = new RequestCulture("en");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });

            builder.Services.AddApplicationServices(builder.Configuration);
            builder.Services.AddInfrastructureServices(builder.Configuration);

            builder.Services.AddScoped<StateService>();
            builder.Services.AddScoped<ILinkGenerator, DocumentPreviewLinkGenerator>();
            builder.Services.AddScoped<DocumentCartService>();

            builder.Services.AddApplicationInsightsTelemetry();
            builder.Services.AddHttpContextAccessor();

            var app = builder.Build();
            ///CHECK HERE
            var RLopt = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>();
            app.UseRequestLocalization(RLopt.Value);

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAzureStorageMappings(builder.Configuration);
            app.UseRouting();

            app.UseCookiePolicy();

            app.MapControllers();
            app.MapGet(PreviewFileEndpoint.Route, 
                async (string fileName,
                    IHttpClientFactory httpClient,
                    CancellationToken cancellationToken) => 
                await PreviewFileEndpoint.DownloadInlineFile(fileName, httpClient, cancellationToken))
               .WithName(PreviewFileEndpoint.EndpointName);

            app.MapGet(AzureBlobEndpoint.Route,
                async (string index, string container, string filename,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                await AzureBlobEndpoint.DownloadInlineFile(index, container, filename, mediator, cancellationToken))
               .WithName(AzureBlobEndpoint.EndpointName);

            app.MapGet(DownloadFileEndpoint.Route,
                async (string fileName,
                    IStorageService storageClient,
                    CancellationToken cancellationToken) =>
                await DownloadFileEndpoint.DownloadInlineFile(fileName, storageClient, cancellationToken))
               .WithName(DownloadFileEndpoint.EndpointName);

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Run();
        }
    }
}