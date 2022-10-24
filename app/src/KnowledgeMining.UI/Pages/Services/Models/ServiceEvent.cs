using KnowledgeMining.Domain.Entities.Jobs;
using KnowledgeMining.Domain.Enums;

namespace KnowledgeMining.UI.Pages.Services.Models
{
    public record ServiceDocumentJobRequestResponse(bool Created, DocumentJobRequest? Request);
}
