using Microsoft.AspNetCore.Components.Forms;

namespace KnowledgeMining.UI.Pages.Documents.ViewModels
{
    public class UploadDocumentViewModel
    {
        public IBrowserFile? File { get; set; }
        public string? DocumentType { get; set; }
    }
}
