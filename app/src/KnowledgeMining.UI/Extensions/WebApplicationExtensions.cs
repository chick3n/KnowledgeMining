using Microsoft.Extensions.FileProviders;

namespace KnowledgeMining.UI.Extensions
{
    public static class WebApplicationExtensions
    {
        public static WebApplication UseAzureStorageMappings(this WebApplication app, IConfiguration configuration)
        {
            var mappings = configuration.GetSection("AzureStorageMappings").Get<string[]>();

            if (mappings != null)
            {
                foreach(var mapping in mappings)
                {
                    var path = $"/{mapping.TrimStart('/')}";
                    app.UseStaticFiles(new StaticFileOptions
                    {
                        FileProvider = new PhysicalFileProvider(path),
                        RequestPath = new PathString(path),
                        ServeUnknownFileTypes = true
                    });
                }
            }

            return app;
        }
    }
}
