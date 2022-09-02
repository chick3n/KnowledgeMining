using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.UI.Extensions;
using System.Web;

namespace KnowledgeMining.UI.Api
{
    public static class PreviewFileEndpoint
    {
        public const string Route = "{index}/api/documents/preview/{fileName}";
        public const string EndpointName = "preview";

        public static async Task<IResult> DownloadInlineFile(
            string index,
            string fileName,
            IStorageService storageService,
            CancellationToken cancellationToken)
        {
            var decodedFileName = HttpUtility.UrlDecode(fileName);
            var fileContents = await storageService.DownloadSource(decodedFileName.Trim('/'), cancellationToken);

            var contentType = FileExtensions.GetContentTypeForFileExtension(decodedFileName.GetFileExtension());

            return Results.Extensions.InlineFile(fileContents, decodedFileName, contentType, false, cancellationToken);
        }
    }
}
