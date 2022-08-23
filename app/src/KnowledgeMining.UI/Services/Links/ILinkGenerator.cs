using KnowledgeMining.Domain.Entities;

namespace KnowledgeMining.UI.Services.Links
{
    public interface ILinkGenerator
    {
        string GenerateDocumentPreviewUrl(DocumentMetadata document);
    }
}
