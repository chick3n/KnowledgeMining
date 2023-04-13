using KnowledgeMining.Application.Documents.Queries.GetDocument;
using KnowledgeMining.Application.Documents.Queries.GetIndex;
using KnowledgeMining.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Text.Json;
using KnowledgeMining.UI.Models;
using KnowledgeMining.UI.Pages.ErrorDocument.Components;
using KnowledgeMining.Application.Documents.Commands.DeleteDocument;
using KnowledgeMining.Application.Common.Exceptions;
using Microsoft.Extensions.Localization;
using System;

namespace KnowledgeMining.UI.Pages.ErrorDocument
{
    public partial class ErrorDocument
    {
        [Inject] public ISnackbar Snackbar { get; set; }
        [Inject] public IMediator Mediator { get; set; }
        [Inject] public IJSRuntime jsRuntime { get; set; }
        [Inject] public IDialogService DialogService { get; set; }
        [Inject] public IStringLocalizer<SharedResources> Localizer { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }

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
        private bool SaveButtonDisabled { get; set; }

        private const int LIST_ITEM_VALUE_RECORD = 1;
        private const int LIST_ITEM_VALUE_SOURCE = 3;
        private const int LIST_ITEM_VALUE_METADATA = 4;
        private readonly string[] INLINE_DOCUMENT_DISPLAY_EXTENSIONS = new string[] { "pdf" };
        private readonly string[] INLINE_DOCUMENT_DOWNLOAD_EXTENSIONS = new string[] { "docx", "doc", "xlsx", "xls", "msg", "eml" };

        protected override async Task OnInitializedAsync()
        {
            _ = Index ?? throw new ArgumentNullException(nameof(Index));
            _ = DocumentName ?? throw new ArgumentNullException(nameof(DocumentName));
            var valueBytes = Convert.FromBase64String(DocumentName);
            DocumentName = System.Text.Encoding.UTF8.GetString(valueBytes);

            await GetIndexItem();
            await GetErrorDocument();

            _indexItem.DeserializedErrorHints = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Localization>>>(_indexItem.ErrorHints.ToString());

            DisplayFilename = _document.Name;
            FilenameChangeWarningIconClass = "row-item invisible";
            SaveButtonDisabled = true;

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

        /// <summary>
        /// Gets document information (including metadata) from the error storage container.
        /// </summary>
        /// <returns></returns>
        private async Task GetErrorDocument()
        {
            var documentResponse = await Mediator.Send(new GetDocumentQuery(_indexItem.Storage.Key, _indexItem.Storage.ErrorContainer, DocumentName));

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
                    // Forcefully go back to the admin page. Avoids issues if the language was changed before the file is deleted.
                    NavigationManager.NavigateTo(
                        NavigationManager.BaseUri + "/" + Index + "/admin/",
                        forceLoad: true
                    );
                }
            }
            else
            {
                if (_canNavigateBack)
                {
                    // Forcefully go back to the admin page. Avoids issues if the language was changed before the file is deleted.
                    NavigationManager.NavigateTo(
                        NavigationManager.BaseUri + "/" + Index + "/admin/",
                        forceLoad: true
                    );
                }
            }
        }

        /*
        private async void DisplayHintDialog(ErrorHelper.Error error)
        {
            var options = new DialogOptions { CloseOnEscapeKey = false };
            var parameters = new DialogParameters {
                ["ErrorName"] = ErrorHelper.GetErrorName(error, _indexItem),
                ["Hint"] = ErrorHelper.GetErrorHint(error, _indexItem)
            };

            var dialog = DialogService.Show<ConfirmDeleteDialogComponent>("Error Hint", parameters, options);
            var result = await dialog.Result;
        }
        */

        /// <summary>
        /// Marks document for deletion in the error storage container, displays a Snackbar indicating success and calls the GoBack method to return to the list of error files.
        /// </summary>
        private async void DeleteDocument()
        {
            var options = new DialogOptions { CloseOnEscapeKey = false };
            var parameters = new DialogParameters { ["Filename"]=_document.Name };
            
            var dialog = DialogService.Show<ConfirmDeleteDialogComponent>("Edit Document Name", parameters, options);
            var result = await dialog.Result;

            if (!result.Cancelled)
            {
                _containsUnsavedChanges = false;

                try
                {
                    // TODO: Use current filename (Justin)
                    await Mediator.Send(new DeleteErrorDocumentCommand(_document.Name, _indexItem.Storage.ErrorContainer, _indexItem.Storage.Key));

                    Snackbar.Add(_document.Name + " was marked to be deleted. This may take a few minutes.", Severity.Success);
                }
                catch
                {
                    Snackbar.Add(_document.Name + " could not be deleted.", Severity.Error);
                }
                
                await GoBack();
            }
        }

        /// <summary>
        /// Save changes made to the document.
        /// 
        /// Includes renaming the document and moving the document from the error container to the document's source container.
        /// </summary>
        private async void Save()
        {           
            try
            {
                // Rename the file
                await Mediator.Send(new MoveDocumentCommand(_indexItem.Storage.Key, _indexItem.Storage.ErrorContainer, _document.Name, _document.Metadata["source_container"], DisplayFilename));
                Snackbar.Add(_document.Name + " was renamed.", Severity.Success);
                
                _containsUnsavedChanges = false;
                await GoBack();
            }
            catch (StorageServiceOperationException ex)
            {
                Snackbar.Add(_document.Name + " could not be renamed and/or resubmitted for ingestion. Try again.", Severity.Error);
            }
        }

        /// <summary>
        /// Method called when the value of the document name input field changes. Sets the flags for warnings of unsaved changes (dialogs and icons) and enables the save button.
        /// </summary>
        /// <param name="newName">New name to assign to the document.</param>
        private void UpdateDisplayFilename(string newName)
        {
            // Display warning icon indicating that the filename was changed but not saved.
            FilenameChangeWarningIconClass = "row-item";
            DisplayFilename = newName;
            _containsUnsavedChanges = true;
            SaveButtonDisabled = false;
        }
    }
}
