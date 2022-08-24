using KnowledgeMining.Application.Common.Options;
using KnowledgeMining.Domain.Entities;
using KnowledgeMining.UI.Api;
using Microsoft.Extensions.Options;

namespace KnowledgeMining.UI.Services.Links
{
    public class DocumentPreviewLinkGenerator : ILinkGenerator
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly LinkGenerator _linker;
        private readonly AzureSignalROptions _azureSignalROptions;

        public DocumentPreviewLinkGenerator(
            LinkGenerator linker,
            IOptions<AzureSignalROptions> azureSignalROptions,
            IHttpContextAccessor httpContextAccessor)
        {
            _linker = linker;
            _httpContextAccessor = httpContextAccessor;
            _azureSignalROptions = azureSignalROptions.Value;
        }

        private string CleanSourcePath(string sourcePath)
        {
            var lastIndex = sourcePath.LastIndexOf('/');
            if (lastIndex > -1 && sourcePath.Length > lastIndex)
                return sourcePath.Substring(lastIndex + 1);

            return sourcePath; 
        }

        public string GenerateDocumentDownloadUrl(DocumentMetadata document)
        {
            if (string.IsNullOrWhiteSpace(document.SourcePath))
                return GenerateDocumentPreviewUrl(DownloadFileEndpoint.EndpointName, document.Name);

            var sourcePath = CleanSourcePath(document.SourcePath);

            return GenerateDocumentPreviewUrl(DownloadFileEndpoint.EndpointName, sourcePath);
        }

        public string GenerateDocumentPreviewUrl(DocumentMetadata document)
        {
            if (string.IsNullOrWhiteSpace(document.SourcePath))
                return GenerateDocumentPreviewUrl(document.Name);

            var sourcePath = CleanSourcePath(document.SourcePath);

            return GenerateDocumentPreviewUrl(sourcePath);
        }

        public string GenerateDocumentPreviewUrl(string documentName)
        {
            return GenerateDocumentPreviewUrl(PreviewFileEndpoint.EndpointName, documentName);
        }

        public string GenerateDocumentPreviewUrl(string endpoint, string documentName)
        {
            var relativePath = _linker.GetPathByName(endpoint, values: new { fileName = documentName });

            if (_azureSignalROptions.Enabled)
            {
                return relativePath!;
            }
            else
            {
                var link = new Uri($"{_httpContextAccessor?.HttpContext?.Request.Scheme}{Uri.SchemeDelimiter}{_httpContextAccessor?.HttpContext?.Request.Host}{relativePath}");

                return link.AbsoluteUri;
            }
        }
    }
}
