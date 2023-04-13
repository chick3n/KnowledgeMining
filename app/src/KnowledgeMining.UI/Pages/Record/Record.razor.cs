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
using KnowledgeMining.UI.Services.State;
using System.Text;
using KnowledgeMining.UI.Models;

namespace KnowledgeMining.UI.Pages.Record
{
    public partial class Record
    {
        [Inject] public ISnackbar Snackbar { get; set; }
        [Inject] public IMediator Mediator { get; set; }
        [Inject] public IJSRuntime jsRuntime { get; set; }
        [Inject] public DocumentCartService CartService { get; set; }

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
        private AzureBlobConnector? _azureBlobConnector;

        private const int LIST_ITEM_VALUE_RECORD = 1;
        private const int LIST_ITEM_VALUE_SOURCE = 3;
        private const int LIST_ITEM_VALUE_METADATA = 4;
        private readonly string[] INLINE_DOCUMENT_DISPLAY_EXTENSIONS = new string[] { "pdf" };
        private readonly string[] INLINE_DOCUMENT_DOWNLOAD_EXTENSIONS = new string[] { "docx", "doc", "xlsx", "xls", "msg", "eml" };

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

            // Get file metadata
            await GetSourceFile();

            _documentMetadataIsLoading = false;

            StateHasChanged();
        }

        /// <summary>
        /// Gets the source file content from a storage container in Azure only (field is metadata_storage_path)
        /// </summary>
        /// <param name="documentMetadata"></param>
        /// <returns></returns>
        private async Task GetSourceFile(bool downloadFile = false)
        {
            if (_indexItem.Storage != null && _indexItem.Storage.AllowSync && _documentMetadata != null)
            {
                var sourcePath = GetSourceFilePath(_documentMetadata);

                if (sourcePath == null || !IsAzureStorageBlobUrl(sourcePath))
                    return;

                var container = GetSourceFileContainer(sourcePath);
                var filename = GetSourceFileName(sourcePath);
                var storage = _indexItem.Storage;
                if (!string.IsNullOrEmpty(storage.Key))
                {
                    var sourceDocument =
                        await Mediator.Send(new GetDocumentQuery(_indexItem.Storage.Key, container, filename));

                    if (sourceDocument.Document.Metadata != null)
                    {
                        if (_documentMetadata.ExtensionData == null)
                            _documentMetadata.ExtensionData = new Dictionary<string, JsonElement>();

                        foreach (var kvp in sourceDocument.Document.Metadata)
                            _documentMetadata.ExtensionData.TryAdd(kvp.Key, JsonSerializer.SerializeToElement(kvp.Value));
                    }

                    _azureBlobConnector = new AzureBlobConnector(Index, container, filename);
                }
            }
        }

        /// <summary>
        /// Get the metadata_storage_path and decode it if necessary
        /// </summary>
        /// <param name="documentMetadata"></param>
        /// <returns></returns>
        private string? GetSourceFilePath(DocumentMetadata? documentMetadata)
        {
            if (documentMetadata == null || documentMetadata.ExtensionData == null)
                return null;

            if (!documentMetadata.ExtensionData.TryGetValue("metadata_storage_path", out var sourcePathElement))
                return null;

            var sourcePath = sourcePathElement.GetString();

            if (string.IsNullOrEmpty(sourcePath))
                return null;

            // handle base64 padding from https://learn.microsoft.com/en-us/azure/search/search-indexer-field-mappings#base64-encoding-options
            var base64SourcePath = Helpers.Base64Helper.UrlTokenToBase64(sourcePath);
            return Helpers.Base64Helper.DecodeToString(base64SourcePath);
        }

        /// <summary>
        /// Given a URL Azure Blob Resource get the container, folders not supported yet
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string GetSourceFileContainer(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                var chunks = path.Split('/');
                if (chunks.Length > 2)
                    return chunks[chunks.Length - 2];
            }

            throw new ArgumentException($"Source Path missing container. {path}");
        }

        /// <summary>
        /// Given a URL Azure Blob Resource get the file name
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private string GetSourceFileName(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                var chunks = path.Split('/');
                if (chunks.Length > 0)
                    return chunks[chunks.Length - 1];
            }

            throw new ArgumentException($"Source Path missing filename. {path}");
        }

        private bool IsAzureStorageBlobUrl(string? path)
        {
            return !string.IsNullOrEmpty(path) && 
                path.Contains(".blob.core.windows.net") && path.StartsWith("https://");
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

        /// <summary>
        /// Should display content as inline source document (ex: PDF)
        /// </summary>
        /// <returns></returns>
        private bool IsInlineContentType()
        {
            return INLINE_DOCUMENT_DISPLAY_EXTENSIONS.Contains(_documentMetadata?.SourceType ?? string.Empty) 
                && _azureBlobConnector != null;
        }

        /// <summary>
        /// Determine if we need to show a download link for the source (ex: Word)
        /// </summary>
        /// <returns></returns>
        private bool ShowDownloadLink()
        {
            return INLINE_DOCUMENT_DOWNLOAD_EXTENSIONS.Contains(GetSourceFileExtension())
                && _azureBlobConnector != null;
        }

        /// <summary>
        /// Determines if the record content is meant to be displayed with markeup
        /// </summary>
        /// <returns></returns>
        private bool ShowMarkupContent()
        {
            return _documentMetadata?.SourceType?.Equals("html") ?? false;
        }

        /// <summary>
        /// Get the source file extension from the metadata
        /// </summary>
        /// <returns></returns>
        private string GetSourceFileExtension()
        {
            if(_documentMetadata == null) return string.Empty;
            var filename = _documentMetadata.SourcePath ??
                _documentMetadata.Name ??
                _documentMetadata.SourceType ??
                string.Empty;

            var extension = Path.GetExtension(filename);
            return extension.TrimStart('.');
        }

        private async Task AddToCart(string title, string recordId)
        {
            var result = await CartService.Add(Index, title, recordId);
            if (result)
            {
                Snackbar.Add("Added to cart!", Severity.Success);
            }
        }
    }
}
