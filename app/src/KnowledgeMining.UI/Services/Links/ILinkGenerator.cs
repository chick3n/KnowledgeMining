using KnowledgeMining.Domain.Entities;
using KnowledgeMining.UI.Models;

namespace KnowledgeMining.UI.Services.Links
{
    public interface ILinkGenerator
    {
        string GenerateDocumentPreviewUrl(DocumentMetadata document);
        string GenerateDocumentDownloadUrl(DocumentMetadata document);
        string GenerateAzureBlobUrl(DocumentMetadata document, AzureBlobConnector azureBlobConnector);
    }
}
