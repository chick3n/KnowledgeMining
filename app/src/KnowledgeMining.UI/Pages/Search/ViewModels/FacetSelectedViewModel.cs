using KnowledgeMining.Application.Documents.Queries.SearchDocuments;

namespace KnowledgeMining.UI.Pages.Search.ViewModels
{
    public record FacetSelectedViewModel(Facet? Facet, bool IsSelected = false);
}
