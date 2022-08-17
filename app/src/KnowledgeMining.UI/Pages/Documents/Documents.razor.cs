using KnowledgeMining.Application.Documents.Commands.DeleteDocument;
using KnowledgeMining.Application.Documents.Commands.EditDocument;
using KnowledgeMining.Application.Documents.Queries.GetDocumentContent;
using KnowledgeMining.Application.Documents.Queries.GetDocumentMetadata;
using KnowledgeMining.Application.Documents.Queries.GetDocuments;
using KnowledgeMining.Application.Documents.Queries.SearchDocuments;
using KnowledgeMining.Domain.Entities;
using KnowledgeMining.UI.Pages.Documents.Componenents;
using KnowledgeMining.UI.Services.Documents;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Caching.Memory;
using MudBlazor;

namespace KnowledgeMining.UI.Pages.Documents
{
    public partial class Documents
    {
        [Inject] public ISnackbar Snackbar { get; set; }
        [Inject] public IDialogService DialogService { get; set; }
        [Inject] public DocumentCacheService DocumentCacheService { get; set; }
        [Inject] public IMediator Mediator { get; set; }

        private bool _isLoading;
        private string? _searchText;
        private int _totalPages = 0;
        private int _pageSize = 25;
        private Document _backupSelectedDocument;
        private IEnumerable<Document> _documents = new List<Document>();

        public async Task OpenUploadComponent()
        {
            //_isUploadComponentVisible = true;
            var options = new DialogOptions { CloseOnEscapeKey = true };
            var dialog = DialogService.Show<UploadDocumentsComponent>("", options);
            var result = await dialog.Result;

            if(!result.Cancelled && result.Data != null)
            {
                if (result.DataType == typeof(IEnumerable<Document>))
                {
                    var results = result.Data as IEnumerable<Document>;
                    await OnFilesUploaded(results!);
                }
            }
        }

        protected override async Task OnInitializedAsync()
        {
            await Search(_searchText);
        }

        private void BackupDocument(Document item)
        {
            _backupSelectedDocument = new Document(item.Name, item.Tags);
        }

        private void RestoreDocument(Document item)
        {
            item = _backupSelectedDocument;
        }

        private void SaveDocument(Document item)
        {
            // TODO: Save changes
        }

        private async ValueTask ReadMoreDocument(Document document)
        {
            //var documentMetaData = await Mediator.Send(new GetDocumentMetadataQuery(document.Name));
            //var searchDocument = await Mediator.Send(new SearchDocumentsQuery($"\"{document.Name}\"", 0, string.Empty, new List<FacetFilter>()));
            //var documentMetaData = searchDocument.Documents.FirstOrDefault();
            //var documentContent = await Mediator.Send(new GetDocumentContentQuery(document.Name));

            var parameters = new DialogParameters { ["document"] = document };
            var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true };
            var dialog = DialogService.Show<ReadDocumentDialogComponent>("Read Document", parameters, options);
            var result = await dialog.Result;
            if (result.Cancelled)
            {
                return;
            }
        }

        private async ValueTask EditDocumentTags(Document document)
        {
            var parameters = new DialogParameters { ["document"] = document };
            var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseOnEscapeKey = true };
            var dialog = DialogService.Show<EditDocumentTagsComponent>("Edit Tags", parameters, options);
            var result = await dialog.Result;
            if (result.Cancelled)
            {
                return;
            }

            if (result.Data != null && result.DataType == typeof(Document))
            {
                var updatedDocument = (Document)result.Data;
                var response = await Mediator.Send(new SetDocumentTraitsCommand(updatedDocument, Domain.Enums.DocumentTraits.Tags));

                if (!response)
                {
                    Snackbar.Add("Failed to update document tags", Severity.Error);
                }
                else
                {
                    await DocumentCacheService.UpdateDocument(updatedDocument);
                    await Search(_searchText);
                }
            }
        }

        private async ValueTask EditDocumentMetadata(Document document)
        {
            var parameters = new DialogParameters { ["document"] = document };
            var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseOnEscapeKey = true };
            var dialog = DialogService.Show<EditDocumentMetadataComponent>("Edit Metadata", parameters, options);
            var result = await dialog.Result;
            if (result.Cancelled)
            {
                return;
            }

            if (result.Data != null && result.DataType == typeof(Document))
            {
                var updatedDocument = ((Document)result.Data);
                var response = await Mediator.Send(new SetDocumentTraitsCommand(updatedDocument, Domain.Enums.DocumentTraits.Metadata));

                if (!response)
                {
                    Snackbar.Add("Failed to update document metadata", Severity.Error);
                }
                else
                {
                    await DocumentCacheService.UpdateDocument(updatedDocument);
                    await Search(_searchText);
                }
            }
        }

        private async ValueTask DeleteDocument(Document document)
        {
            var parameters = new DialogParameters { ["document"] = document };
            var dialog = DialogService.Show<DeleteDocumentDialogComponent>("Delete Document", parameters);
            var result = await dialog.Result;
            if (result.Cancelled)
            {
                return;
            }

            try
            {
                await Mediator.Send(new DeleteDocumentCommand(document.Name));
                DocumentCacheService.RemoveDocuments(x => !x.Name.Equals(document.Name));
                await Search(_searchText);

                Snackbar.Add("Document deleted", Severity.Success);
            }
            catch
            {
                Snackbar.Add("Failed to deleted Document", Severity.Error);
            }
        }

        private async Task OnSearch(string searchText)
        {
            _searchText = searchText;
            await Search(searchText);
        }

        private async Task Search(string? searchText)
        {
            _totalPages = 0;

            _isLoading = true;

            var cachedDocuments = DocumentCacheService.GetDocuments();
            var query = cachedDocuments.Where(x => x.Name.Contains(searchText ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            _totalPages = query.Count();
            _documents = query.ToList();

            _isLoading = false;

            UpdateTable(_documents);
        }

        private void UpdateTable(IEnumerable<Document> documents)
        {
            _documents = documents;
        }

        private async Task RefreshCacheClicked(MouseEventArgs args)
        {
            _isLoading = true;

            Snackbar.Add("Refreshing Document Table...", Severity.Warning);

            UpdateTable(new List<Document>());

            var documents = await DocumentCacheService.BuildCache(new CancellationToken());

            UpdateTable(documents);

            _isLoading = false;

            Snackbar.Add("Refreshing Document Table Completed", Severity.Success);
        }

        public async Task OnFilesUploaded(IEnumerable<Document> documents)
        {
            if (documents.Count() > 0)
            {
                await DocumentCacheService.AddDocuments(documents);
                await Search(_searchText);
            }
        }

    }
}