using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.UI.Extensions;
using System.Web;

namespace KnowledgeMining.UI.Api
{
    public static class DownloadFileEndpoint
    {
        public const string Route = "api/documents/download/{fileName}";
        public const string EndpointName = "download";

        public static async Task<IResult> DownloadInlineFile(
            string fileName,
            IStorageService storageService,
            CancellationToken cancellationToken)
        {
            var decodedFileName = HttpUtility.UrlDecode(fileName);
            var fileContents = await storageService.DownloadSource(decodedFileName.Trim('/'), cancellationToken);

            var contentType = FileExtensions.GetContentTypeForFileExtension(decodedFileName.GetFileExtension());

            return Results.Extensions.InlineFile(fileContents, decodedFileName, contentType, true, cancellationToken);
        }
    }
}
