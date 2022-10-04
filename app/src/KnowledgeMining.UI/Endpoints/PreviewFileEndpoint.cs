using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.UI.Extensions;
using System.Web;

namespace KnowledgeMining.UI.Api
{
    public static class PreviewFileEndpoint
    {
        public const string Route = "/preview/{filename}";
        public const string EndpointName = "preview";

        public static async Task<IResult> DownloadInlineFile(
            string fileName,
            IHttpClientFactory httpClient,
            CancellationToken cancellationToken)
        {
            var decodedFileName = HttpUtility.UrlDecode(fileName);

            var client = httpClient.CreateClient(PreviewFileEndpoint.EndpointName);
            var fileContents = default(byte[]);

            using (var stream = await client.GetStreamAsync(decodedFileName))
            using (var memStream = new MemoryStream()) {
                stream.CopyTo(memStream);
                fileContents = memStream.ToArray();    
            }

            var contentType = FileExtensions.GetContentTypeForFileExtension(decodedFileName.GetFileExtension());
            return Results.Extensions.InlineFile(fileContents, decodedFileName, contentType, false, cancellationToken);
        }
    }
}
