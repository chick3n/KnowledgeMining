using KnowledgeMining.Application.Documents.Queries.GetDocumentMetadata;
using KnowledgeMining.Application.Documents.Queries.GetDocument;
using KnowledgeMining.Application.Documents.Queries.GetIndex;
using KnowledgeMining.Domain.Entities;
using KnowledgeMining.UI.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Text.Json.Nodes;
using System.Text.Json;
using KnowledgeMining.Application.Documents.Queries.SearchDocuments;

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
        private bool _documentMetadataIsLoading = true;
        private bool _relatedDocumentMetadataIsLoading = true;
        private MudListItem _selectedItem;
        private object _selectedValue = 1;
        private bool _canNavigateBack = false;

        //UI
        private string _title = string.Empty;

        //Functional
        private DocumentMetadata? _documentMetadata;
        private IndexItem? _indexItem;
        private string? _textToHighlight;
        private DocumentMetadataWrapper? _moreLikeThis;

        private const int LIST_ITEM_VALUE_RECORD = 1;
        private const int LIST_ITEM_VALUE_SOURCE = 3;
        private const int LIST_ITEM_VALUE_METADATA = 4;

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
                StateHasChanged();
            }
        }

        protected override Task OnParametersSetAsync()
        {
            if(_documentMetadataIsLoading)
                GetRecordDetails().ConfigureAwait(false);

            if(_relatedDocumentMetadataIsLoading)
                GetMoreLikeThis().ConfigureAwait(false);

            return base.OnParametersSetAsync();
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            if(parameters.TryGetValue<string>("RecordId", out var recordId))
            {
                if(RecordId == null || !RecordId.Equals(recordId))
                {
                    _documentMetadataIsLoading = _relatedDocumentMetadataIsLoading = true;
                }
            }

            return base.SetParametersAsync(parameters);
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
            _documentMetadataIsLoading = true;

            var documentMetadata = await Mediator.Send(new GetDocumentMetadataQuery(_indexItem!.IndexName!, RecordId));
            var wrapper = new DocumentMetadataWrapper(new DocumentMetadata[] { documentMetadata },
                _indexItem.FieldMapping, _indexItem.KeyField);
            _documentMetadata = wrapper.Documents().FirstOrDefault();
            _title = wrapper.GetTitle(_documentMetadata) ?? string.Empty;

            if (_indexItem.Storage != null && _indexItem.Storage.AllowSync && _documentMetadata != null)
            {
                var storage = _indexItem.Storage;
                if (!string.IsNullOrEmpty(storage.Container) && !string.IsNullOrEmpty(storage.Key) && !string.IsNullOrEmpty(_documentMetadata.Name))
                {
                    var sourceDocument =
                        await Mediator.Send(new GetDocumentQuery(_indexItem.Storage.Key, _indexItem.Storage.Container, _documentMetadata.Name));

                    if(sourceDocument.Document.Metadata != null)
                    {
                        if (_documentMetadata.ExtensionData == null)
                            _documentMetadata.ExtensionData = new Dictionary<string, JsonElement>();

                        foreach (var kvp in sourceDocument.Document.Metadata)
                            _documentMetadata.ExtensionData.TryAdd(kvp.Key, JsonSerializer.SerializeToElement(kvp.Value));
                    }
                }
            }

            _documentMetadataIsLoading = false;

            StateHasChanged();
        }

        private async Task GetMoreLikeThis()
        {
            _relatedDocumentMetadataIsLoading = true;

            var indexKey = _indexItem?.KeyField ?? throw new ArgumentNullException("IndexConfig.Key");
            var indexName = _indexItem?.Id ?? throw new ArgumentNullException("IndexConfig.Id");

            var moreLikeThis = await Mediator.Send(new MoreLikeThisQuery(indexName, 
                RecordId,
                new[] { "title", indexKey },
                new[] { "content" }));

            _moreLikeThis = new DocumentMetadataWrapper(
                moreLikeThis.Documents
                    .OrderByDescending(x => x.SearchScore)
                    .Take(5),
                _indexItem.FieldMapping, _indexItem.KeyField);

            _relatedDocumentMetadataIsLoading = false;

            StateHasChanged();
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

        private string? UriEncodedSourcePath()
        {
            if(_documentMetadata != null && !string.IsNullOrWhiteSpace(_documentMetadata?.SourcePath))
            {
                if (Uri.TryCreate(_documentMetadata?.SourcePath, UriKind.Absolute, out var uri))
                {
                    return uri.AbsoluteUri;
                }
            }
            return null;
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

        private bool IsPreviewType()
        {
            if (!string.IsNullOrWhiteSpace(_documentMetadata?.SourcePath))
            {
                var ext = Path.GetExtension(_documentMetadata?.SourcePath);
                switch(ext?.ToLower() ?? string.Empty)
                {
                    case ".pdf": return true;
                }
            }

            return false;
        }
    }
}
