using KnowledgeMining.Application.Documents.Queries.GetDatabaseItem;
using KnowledgeMining.Application.Documents.Queries.GetIndex;
using KnowledgeMining.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MetricsData = KnowledgeMining.Domain.Entities.Metrics;

namespace KnowledgeMining.UI.Pages.Metrics
{
    public partial class Metrics
    {
        [Inject] public IMediator? Mediator { get; set; }

        [Parameter] public string Index { get; set; } = default!;

        //Functional
        private IndexItem? _indexItem;
        private MetricsData? _metrics;
        private Random _random = new Random();

        //Display

        //Word Count
        public string? TotalWords;
        public string? TotalWordsExclude;
        public Dictionary<string, int>? TopWords;
        public Dictionary<string, int>? TopWordsExclude;

        //Search
        public string? TotalDocuments;
        public string? IndexSize;
        public Dictionary<string, int>? FileTypes;

        protected override async Task OnInitializedAsync()
        {
            _ = Index ?? throw new ArgumentNullException(nameof(Index));

            await GetIndexItem();
            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if(firstRender)
            {
                await GetMetrics();
                StateHasChanged();
            }
        }

        private async Task GetIndexItem()
        {
            var indexResponse = await Mediator.Send(new GetIndexQuery(Index));
            if (indexResponse.IndexItem == null)
                throw new FileNotFoundException(Index);
            _indexItem = indexResponse.IndexItem;
        }

        private async Task GetMetrics()
        {
            var databaseResponse = await Mediator.Send(new GetDatabaseItemMetricsQuery(_indexItem.IndexName));
            if (databaseResponse.Metrics == null)
                throw new FileNotFoundException(Index);
            _metrics = databaseResponse.Metrics;

            BuildWordMetrics();
        }

        private void BuildWordMetrics()
        {
            if (_metrics == null) return;

            if (_metrics.WordCount != null)
            {
                TotalWords = _metrics.WordCount.Count?.ToString("N0");
                TotalWordsExclude = _metrics.WordCount.CountExcludeStopWords?.ToString("N0");
                TopWords = _metrics.WordCount.Top;
                TopWordsExclude = _metrics.WordCount.TopExcludeStopWords;
            }

            if (_metrics.Search != null)
            {
                TotalDocuments = _metrics.Search.DocumentCount?.ToString();
                IndexSize = _metrics.Search.IndexSize;
                FileTypes = _metrics.Search.FileTypes?.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, y => y.Value);
                var x = FileTypes.Skip(0).Take(8).ToList();
            }
        }

        private string GetColorAsHex()
        {
            return string.Format("#{0:X6}", _random.Next(0x1000000));
        }

        private string GetFileTypeIcon(string filetype)
        {
            if (string.IsNullOrEmpty(filetype))
                return Icons.Custom.FileFormats.FileDocument;

            filetype = filetype.TrimStart('.');
            switch(filetype.ToLower())
            {
                case "excel":
                case "xlsx":
                case "xls":
                    return Icons.Custom.FileFormats.FileExcel;
                case "email":
                case "mail":
                case "msg":
                    return Icons.Filled.Email;
                case "txt":
                case "text":
                case "plaintext":
                    return Icons.Custom.FileFormats.FileDocument;
                case "pdf":
                    return Icons.Custom.FileFormats.FilePdf;
                case "doc":
                case "word":
                case "docx":
                    return Icons.Custom.FileFormats.FileWord;
                case "image":
                case "png":
                case "gif":
                case "bmp":
                case "jpeg":
                case "jpg":
                    return Icons.Custom.FileFormats.FileImage;
            }

            return Icons.Custom.FileFormats.FileDocument;
        }
    }
}
