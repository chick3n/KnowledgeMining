using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.Application.Documents.Queries.GetDocument;
using KnowledgeMining.Application.Documents.Queries.GetIndex;
using KnowledgeMining.UI.Extensions;
using MediatR;
using System.Web;

namespace KnowledgeMining.UI.Api
{
    public static class AzureBlobEndpoint
    {
        public const string Route = "/inline/{index}/{container}/{*filename}";
        public const string EndpointName = "inline";

        public static async Task<IResult> DownloadInlineFile(
            string index,
            string container,
            string filename,
            IMediator mediator,
            CancellationToken cancellationToken)
        {
            var decodedIndex = HttpUtility.UrlDecode(index);
            var decodedContainer = HttpUtility.UrlDecode(container);
            var decodedFileName = HttpUtility.UrlDecode(filename);

            var indexResponse = await mediator.Send(new GetIndexQuery(decodedIndex));
            if (indexResponse.IndexItem == null)
                throw new FileNotFoundException(index);
            var indexItem = indexResponse.IndexItem;

            if(indexItem.Storage != null && indexItem.Storage.AllowSync)
            {
                var sourceDocument =
                        await mediator.Send(new GetDocumentQuery(indexItem.Storage.Key, decodedContainer, decodedFileName, true));

                var contentType = FileExtensions.GetContentTypeForFileExtension(decodedFileName.GetFileExtension());
                return Results.Extensions.InlineFile(sourceDocument.Document.rawContent, decodedFileName, contentType, false, cancellationToken);
            }

            throw new FileNotFoundException(nameof(decodedFileName));
        }
    }
}
