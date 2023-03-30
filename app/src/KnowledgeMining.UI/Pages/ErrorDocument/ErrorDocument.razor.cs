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
using KnowledgeMining.UI.Pages.Record;
using KnowledgeMining.UI.Pages.ErrorDocument.Components;

namespace KnowledgeMining.UI.Pages.ErrorDocument
{
    public partial class ErrorDocument
    {
        [Inject] public ISnackbar Snackbar { get; set; }
        [Inject] public IMediator Mediator { get; set; }
        [Inject] public IJSRuntime jsRuntime { get; set; }
        [Inject] public DocumentCartService CartService { get; set; }
        [Inject] public IDialogService DialogService { get; set; }

        [Parameter] public string Index { get; set; } = default!;
        [Parameter] public string DocumentName { get; set; } = null!;


        //UI States
        private bool _documentIsLoading = true;
        private bool _canNavigateBack = false;
        private bool _containsUnsavedChanges = false;

        //UI
        private string _title = string.Empty;

        //Functional
        private DocumentMetadata? _documentMetadata;
        private IndexItem? _indexItem;
        private AzureBlobConnector? _azureBlobConnector;
        private Document _document;
        private string? DisplayFilename { get; set; }
        private string? FilenameChangeWarningIconClass { get; set; }

        private const int LIST_ITEM_VALUE_RECORD = 1;
        private const int LIST_ITEM_VALUE_SOURCE = 3;
        private const int LIST_ITEM_VALUE_METADATA = 4;
        private readonly string[] INLINE_DOCUMENT_DISPLAY_EXTENSIONS = new string[] { "pdf" };
        private readonly string[] INLINE_DOCUMENT_DOWNLOAD_EXTENSIONS = new string[] { "docx", "doc", "xlsx", "xls", "msg", "eml" };

        protected override async Task OnInitializedAsync()
        {
            _ = Index ?? throw new ArgumentNullException(nameof(Index));
            _ = DocumentName ?? throw new ArgumentNullException(nameof(DocumentName));

            await GetIndexItem();
            await GetErrorDocument();

            DisplayFilename = _document.Name;
            FilenameChangeWarningIconClass = "row-item invisible";

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
            
            return base.OnParametersSetAsync();
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            
            return base.SetParametersAsync(parameters);
        }

        private async Task GetIndexItem()
        {
            var indexResponse = await Mediator.Send(new GetIndexQuery(Index));
            if (indexResponse.IndexItem == null)
                throw new FileNotFoundException(DocumentName);
            _indexItem = indexResponse.IndexItem;
        }

        private async Task GetErrorDocument()
        {
            var documentResponse = await Mediator.Send(new GetDocumentQuery(_indexItem.Storage.Key, "error-documents", DocumentName));

            _document = documentResponse.Document;

            _documentIsLoading = false;
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

        private async Task GoBack()
        {
            if (_containsUnsavedChanges)
            {
                var options = new DialogOptions { CloseOnEscapeKey = false };
                var dialog = DialogService.Show<ConfirmCancelDialogComponent>("Unsaved Changes", options);
                var result = await dialog.Result;

                if (!result.Cancelled && _canNavigateBack)
                {
                    await jsRuntime.InvokeVoidAsync("history.back");
                }
            }
            else
            {
                if (_canNavigateBack)
                {
                    await jsRuntime.InvokeVoidAsync("history.back");
                }
            }
        }

        private int GetNumberOfErrors()
        {
            var errors = _document.Metadata["error"].Split(';');
            return errors.Length;
        }

        /*
        private string GetErrorHint(string errorName, string language)
        {
            return _indexItem.ErrorHints.InvalidDate.FrenchString;
        }
        */

        private async void DisplayConfirmDeleteDialog()
        {
            var options = new DialogOptions { CloseOnEscapeKey = false };
            var parameters = new DialogParameters { ["Filename"]=_document.Name };
            
            var dialog = DialogService.Show<ConfirmDeleteDialogComponent>("Edit Document Name", parameters, options);
            var result = await dialog.Result;

            if (!result.Cancelled)
            {
                _containsUnsavedChanges = false;

                // Delete file from error-document

                // Success Snackbar
                Snackbar.Add(_document.Name + " was deleted.", Severity.Success);
                

                // Failed Snackbar
                Snackbar.Add(_document.Name + " could not be deleted.", Severity.Error);
                await GoBack();
            }
        }

        private async void Save()
        {

            // Rename the file and copy it to source container (using _displayedFilename, source container metadata)
            // Delete file in error-documents
            // display snackbar

            // Success Snackbar
            Snackbar.Add(_document.Name + " was renamed and resubmitted for ingestion.", Severity.Success);
            _containsUnsavedChanges = false;

            // Failed Snackbar
            Snackbar.Add(_document.Name + " could not be renamed and/or resubmitted for ingestion.", Severity.Error);


            await GoBack();
        }

        private void UpdateDisplayFilename(string newName)
        {
            // Display warning icon indicating that the filename was changed but not saved.
            FilenameChangeWarningIconClass = "row-item";
            DisplayFilename = newName;
            _containsUnsavedChanges = true;
        }

        private string GetErrorHint(string errorName)
        {
            return "Test";
        }
    }
}
