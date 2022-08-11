using KnowledgeMining.Application.Documents.Commands.DeleteDocument;
using KnowledgeMining.Application.Documents.Queries.GetDocumentContent;
using KnowledgeMining.Application.Documents.Queries.GetDocumentMetadata;
using KnowledgeMining.Application.Documents.Queries.GetDocuments;
using KnowledgeMining.Application.Documents.Queries.SearchDocuments;
using KnowledgeMining.Domain.Entities;
using KnowledgeMining.UI.Pages.Documents.Componenents;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Caching.Memory;
using MudBlazor;

namespace KnowledgeMining.UI.Pages.Documents
{
    public partial class Documents
    {
        [Inject] public ISnackbar Snackbar { get; set; }
        [Inject] public IDialogService DialogService { get; set; }

        [Inject] public IMediator Mediator { get; set; }

        [Inject] public IMemoryCache MemoryCache { get; set; }

        private bool _isLoading;
        private string? _searchText;
        private int _currentPage = 0;
        private int _totalPages = 0;
        private int _pageSize = 25;
        private Document _backupSelectedDocument;
        private IEnumerable<Document> _documents = new List<Document>();

        // Upload Document
        private bool _isUploadComponentVisible;
        public void OpenUploadComponent()
        {
            _isUploadComponentVisible = true;
        }

        protected override async Task OnInitializedAsync()
        {
            Search(_searchText);
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
                _documents = _documents.Where(x => x.Name.Equals(document.Name));
                MemoryCache.Set(Constants.DOCUMENT_FILTER_CACHE, _documents);
                Search(_searchText);

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
            Search(searchText);
        }

        private void LoadPreviousPage()
        {
            if (_currentPage >= 1)
                _currentPage -= 1;
            UpdateTable(_documents.Skip(_currentPage).Take(_pageSize));
        }

        private void LoadNextPage()
        {
            UpdateTable(_documents.Skip(_currentPage).Take(_pageSize));
            _currentPage += 1;
        }

        private void Search(string? searchText)
        {
            _currentPage = 0;
            _totalPages = 0;

            _isLoading = true;

            MemoryCache.TryGetValue<IEnumerable<Document>>(Constants.DOCUMENT_FILTER_CACHE, out var cachedDocuments);

            if (cachedDocuments == null)
                cachedDocuments = new List<Document>();

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

    }
}