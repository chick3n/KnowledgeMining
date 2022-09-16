using KnowledgeMining.Application.Documents.Queries.GetDocumentMetadata;
using KnowledgeMining.Application.Documents.Queries.GetIndex;
using KnowledgeMining.Domain.Entities;
using KnowledgeMining.UI.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace KnowledgeMining.UI.Pages.Record
{
    public partial class Record
    {
        [Inject] public ISnackbar Snackbar { get; set; }
        [Inject] public IMediator Mediator { get; set; }
        [Inject] public IJSRuntime jsRuntime { get; set; }

        [Parameter] public string Index { get; set; } = default!;
        [Parameter] public string RecordId { get; set; } = null!;

        //UI States
        private bool _isLoading = true;
        private MudListItem _selectedItem;
        private object _selectedValue = 1;
        private bool _canNavigateBack = false;

        //UI
        private string _title = string.Empty;
        private string _contentMinHeight = "85vh";

        //Functional
        private DocumentMetadata? _documentMetadata;
        private IndexItem? _indexItem;
        private string? _textToHighlight;

        protected override async Task OnInitializedAsync()
        {
            _ = Index ?? throw new ArgumentNullException(nameof(Index));
            _ = RecordId ?? throw new ArgumentNullException(nameof(RecordId));

            await GetIndexItem();
            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if(firstRender)
            {
                _canNavigateBack = await jsRuntime.InvokeAsync<bool>("HasHistory");
                await GetRecordDetails();
                FinishedLoading();
                StateHasChanged();
            }
        }

        private void FinishedLoading()
        {
            _isLoading = false;
        }

        private async Task GetIndexItem()
        {
            var indexResponse = await Mediator.Send(new GetIndexQuery(Index));
            if (indexResponse.IndexItem == null)
                throw new FileNotFoundException(RecordId);
            _indexItem = indexResponse.IndexItem;
        }

        private async Task GetRecordDetails()
        {
            var documentMetadata = await Mediator.Send(new GetDocumentMetadataQuery(_indexItem!.IndexName!, RecordId));
            var wrapper = new DocumentMetadataWrapper(new DocumentMetadata[] { documentMetadata },
                _indexItem.FieldMapping, _indexItem.KeyField);
            _documentMetadata = wrapper.Documents().FirstOrDefault();
            _title = wrapper.GetTitle(_documentMetadata) ?? string.Empty;
        }

        //Redo
        private void UpdateTextToHighlight(string searchText)
        {
            _textToHighlight = searchText;
        }

        private void UpdateTextToHighlight(MudChip selectedChip)
        {
            _textToHighlight = selectedChip?.Text ?? string.Empty;
        }

        

        private async Task GoBack()
        {
            if(_canNavigateBack)
                await jsRuntime.InvokeVoidAsync("history.back");
            
        }

        private bool IsUrlPath()
        {
            if (!string.IsNullOrWhiteSpace(_documentMetadata?.SourcePath))
            {
                if (Uri.TryCreate(_documentMetadata?.SourcePath, UriKind.Absolute, out var uri))
                {
                    return uri.Scheme.Equals("http", StringComparison.CurrentCultureIgnoreCase) ||
                        uri.Scheme.Equals("https", StringComparison.CurrentCultureIgnoreCase);
                }
            }

            return false;
        }
    }
}
