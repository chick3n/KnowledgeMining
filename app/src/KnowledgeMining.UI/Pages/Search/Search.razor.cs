using KnowledgeMining.Application.Documents.Queries.GetIndex;
using KnowledgeMining.Application.Documents.Queries.SearchDocuments;
using KnowledgeMining.Application.Documents.Queries.GenerateEntityMap;
using KnowledgeMining.Application.Documents.Queries.GetAutocompleteSuggestions;
using KnowledgeMining.Application.Documents.Queries.GetDocumentMetadata;
using KnowledgeMining.Domain.Entities;
using KnowledgeMining.Infrastructure.Services.Storage;
using KnowledgeMining.UI.Pages.Search.ViewModels;
using KnowledgeMining.UI.Helpers;
using KnowledgeMining.UI.Services.Metadata;
using KnowledgeMining.UI.Services.State;
using KnowledgeMining.UI.Wrappers;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using KnowledgeMining.UI.Pages.Search.Components;
using Microsoft.AspNetCore.Components.Web;
using MediatR;

namespace KnowledgeMining.UI.Pages.Search
{
    public partial class Search
    {
        [Inject] public IMediator? Mediator { get; set; }
        [Inject] public NavigationManager? NavManager { get; set; }
        [Inject] public StateService? StateService { get; set; }
        [Inject] public ILogger<Search>? Logger { get; set; }

        [Parameter] public string? Index { get; set; }
        [Parameter] public string? Hash { get; set; }
        [Parameter, SupplyParameterFromQuery(Name = "q")] public string? SearchText { get; set; }

        private MudDateRangePicker? _timespanPicker;
        private FacetsFilterComponent? _facetsFilterComponent;
        private SearchResultsComponent? _searchResultsComponent;

        private SearchState _searchState = new();

        private int _selectedPage;
        private List<FacetFilter> _selectedFacets = new();
        private List<FacetFilter> _selectedFilters = new();
        private List<FacetFilter> _orderBy = DefaultSortByFilters();
        private string poligonString = string.Empty;
        private enum TimeSpanType
        {
            Any,
            Today,
            Week,
            Month,
            Custom
        };
        private DateTime? _minDate = null;
        private DateTime? _maxDate = null;

        private string[] excludeFacets = new string[] { BlobMetadata.Mission, BlobMetadata.DocumentType };
        private TimeSpanType _timeSpanSelectedType = TimeSpanType.Any;
        private string _timespanLabel = "Any";
        private string _searchResultsLabel = "Search Results";
        private bool _isSearching = true;
        private bool _showDocumentDetails = false;
        private bool ShowDocumentDetails
        {
            get { return _showDocumentDetails; }
            set
            {
                _showDocumentDetails = value;
            }
        }
        private DocumentMetadata? _documentMetadata;
        private IndexItem? _indexItem;
        private const string LABEL_ORDERBY_BEST_MATCH = "Best Match";
        private const string LABEL_ORDERBY_DATE = "Date";
        private string _sortLabel = LABEL_ORDERBY_BEST_MATCH;

        private MudTabs? _mainBodyTabs;
        private MudTabPanel? _searchResultsPanel;

        private string _currentHash;

        private string current_lang = @GetCurrentLanguage.GetLanguageCode();

        protected override async Task OnInitializedAsync()
        {
            var indexResponse = await Mediator.Send(new GetIndexQuery(Index));
            _indexItem = indexResponse.IndexItem;
            ConfigureUI();
        }

        private void ConfigureUI()
        {
            if (_indexItem?.Configuration?.TimeSpan != null)
            {
                _minDate = _indexItem.Configuration.TimeSpan.Start ?? null;
                _maxDate = _indexItem.Configuration.TimeSpan.End ?? null;
            }
        }

        /*protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                if (string.IsNullOrEmpty(Hash))
                {
                    var request = new SearchDocumentsQuery(_indexItem, SearchText, _selectedPage, poligonString, _selectedFacets, _selectedFilters, _orderBy);
                    await SearchDocuments(request);
                }
            }
        }*/

        protected override Task OnParametersSetAsync()
        {
            if (_isSearching)
                SearchDocuments().ConfigureAwait(false);                

            return base.OnParametersSetAsync();
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.TryGetValue<string>("Hash", out var hash))
            {
                if (Hash == null || !Hash.Equals(hash))
                {
                    _isSearching = true;
                }
            }

            return base.SetParametersAsync(parameters);
        }

        #region UI

        private async Task SearchIfButtonClicked(MouseEventArgs args)
        {
            var request = new SearchDocumentsQuery(_indexItem, SearchText, _selectedPage, poligonString, _selectedFacets, _selectedFilters, _orderBy);
            await BeginSearchDocuments(request);
        }

        private async Task SearchIfEnterPressed(KeyboardEventArgs e)
        {
            if (e.Code == "Enter" || e.Code == "NumpadEnter")
            {
                var request = new SearchDocumentsQuery(_indexItem, SearchText, _selectedPage, poligonString, _selectedFacets, _selectedFilters, _orderBy);
                await BeginSearchDocuments(request);
            }
        }

        private async Task SearchIfClearClicked(MouseEventArgs e)
        {
            var request = new SearchDocumentsQuery(_indexItem, SearchText, _selectedPage, poligonString, _selectedFacets, _selectedFilters, _orderBy);
            await BeginSearchDocuments(request);
        }

        private DocumentMetadataWrapper GetDocumentMetadataWrapper()
        {
            return new DocumentMetadataWrapper(_searchState.Documents, _indexItem?.FieldMapping, _searchState.KeyField);
        }

        #endregion

        private FacetFilter GenerateFacetFilter(Facet facet)
        {
            var facetFilter = new FacetFilter()
            {
                Name = facet.Name
            };

            switch (facet.Name)
            {
                case BlobMetadata.Mission:
                    facetFilter.OverrideType = typeof(string[]);
                    break;
            }

            return facetFilter;
        }

        private async Task UpdateSearchFacetsAndSearch(FacetSelectedViewModel facetViewModel)
        {
            var facet = facetViewModel.Facet;
            var isSelected = facetViewModel.IsSelected;

            if (facet == null)
                return;

            var selectedFacet = _selectedFacets.FirstOrDefault(x => x.Name != null && x.Name.Equals(facet.Name));
            
            if (isSelected)
            {
                if (selectedFacet == null)
                {
                    selectedFacet = GenerateFacetFilter(facet);
                    _selectedFacets.Add(selectedFacet);
                }

                if (facet.Value != null && !selectedFacet.Values.Any(x => x.Equals(facet.Value)))
                    selectedFacet.Values.Add(facet.Value);
            }
            else
            {
                if(facet.Value != null && selectedFacet != null && selectedFacet.Values.Any(x => x.Equals(facet.Value)))
                {
                    selectedFacet.Values.Remove(facet.Value);
                }                
            }

            var request = new SearchDocumentsQuery(_indexItem, SearchText, _selectedPage, poligonString, _selectedFacets, _selectedFilters, _orderBy);

            await BeginSearchDocuments(request);
        }

        private async Task SearchPageSelected(int page)
        {
            _selectedPage = page;
            var request = new SearchDocumentsQuery(_indexItem, SearchText, _selectedPage, poligonString, _selectedFacets, _selectedFilters, _orderBy);
            await BeginSearchDocuments(request);
        }

        private async Task BeginSearchDocuments(SearchDocumentsQuery request)
        {
            SwitchToSearchResultsTab();
            var hash = StateService.GenerateHash(SearchQueryStateModel.Hash(request));
            NavManager.NavigateTo($"/{Index}/search/{hash}", forceLoad: false);
        }


        private async Task SearchDocuments()
        {
            if (string.IsNullOrEmpty(Hash))
                await SearchDocumentsEmpty();
            else 
                await SearchDocumentsFromHash();
        }

        private async Task SearchDocumentsEmpty()
        {
            _selectedPage = 0;
            poligonString = string.Empty;
            _selectedFacets.Clear();
            _selectedFilters.Clear();
            _orderBy = DefaultSortByFilters();
            _timeSpanSelectedType = TimeSpanType.Any;
            _facetsFilterComponent?.ClearFacets();

            if (current_lang.Equals("en"))
                _sortLabel = LABEL_ORDERBY_BEST_MATCH;
            else
                _sortLabel = "Meilleure Correspondance";

            UpdateTimeSpanMenu();

            var request =
                new SearchDocumentsQuery(_indexItem, SearchText, _selectedPage, poligonString, _selectedFacets, _selectedFilters, _orderBy);

            await SearchDocuments(request);
        }

        private async Task SearchDocumentsFromHash()
        { 
            SwitchToSearchResultsTab();

            _isSearching = true;

            UpdateSearchResultsLabelWithDocumentCount(default);

            var stateModel = StateService.DecodeHash(Hash);
            var searchQueryStateModel = new SearchQueryStateModel(stateModel);


            if (searchQueryStateModel != null)
            {
                SearchText = searchQueryStateModel.GetSearchText();
                _selectedPage = searchQueryStateModel.Page;
                poligonString = searchQueryStateModel.PolygonString;
                _selectedFacets = searchQueryStateModel.FacetFilters.ToList();
                _selectedFilters = searchQueryStateModel.FieldFilters.ToList();
                _orderBy = searchQueryStateModel.Order.ToList();
            }
            var request = new SearchDocumentsQuery(_indexItem, SearchText, _selectedPage, poligonString, _selectedFacets, _selectedFilters, _orderBy);

            await SearchDocuments(request);
        }

        private async Task SearchDocuments(SearchDocumentsQuery request)
        {
            var response = await Mediator.Send(request);

            _searchState.Documents = response.Documents;
            _searchState.FacetableFields = response.FacetableFields;
            _searchState.TotalCount = response.TotalCount;
            _searchState.TotalPages = (int)response.TotalPages;
            _searchState.Facets = SummarizeFacets(response.Facets.ToArray());
            _searchState.KeyField = response.KeyField;

            UpdateSearchResultsLabelWithDocumentCount(_searchState.TotalCount);

            _isSearching = false;

            StateHasChanged();
        }


        private IEnumerable<SummarizedFacet> SummarizeFacets(SummarizedFacet[] facets)
        {
            if (_indexItem == null || _indexItem.Facets == null)
            {
                return facets;
            }

            foreach (var facet in facets)
            {
                if (facet.Name == null) continue;

                var indexFacet = _indexItem.Facets.FirstOrDefault(x => x.Id.Equals(facet.Name));
                if (indexFacet != null)
                {
                    if (indexFacet.ShowAll && indexFacet.Values != null)
                    {
                        var newValues = facet.Values.ToList();
                        foreach (var value in indexFacet.Values)
                        {
                            if (!facet.Values.Any(x => x.Name.Equals(value.Id)))
                            {
                                newValues.Add(new Facet
                                {
                                    Count = 0,
                                    Name = facet.Name,
                                    Value = value.Id
                                });
                            }
                        }
                        facet.Values = newValues;
                        facet.Count = newValues.Count;
                    }
                }
            }

            return facets;
        }

        private void SwitchToSearchResultsTab()
        {
            if (_mainBodyTabs?.ActivePanel != _searchResultsPanel && _searchResultsPanel is not null)
            {
                _mainBodyTabs?.ActivatePanel(_searchResultsPanel);
            }
        }

        private void UpdateSearchResultsLabelWithDocumentCount(long? documentsCount)
        {
            if ((documentsCount is null || documentsCount <= 0) && current_lang.Equals("en"))
            {
                _searchResultsLabel = "Search Results";
            }
            else if ((documentsCount is null || documentsCount <= 0) && current_lang.Equals("fr"))
            {
                _searchResultsLabel = "Résultats de recherche";
            }
            else if (current_lang.Equals("en"))
            {
                _searchResultsLabel = $"Search Results ({documentsCount})";
            }
            else if (current_lang.Equals("fr"))
            {
                _searchResultsLabel = $"Résultats de recherche ({documentsCount})";
            }
        }

        private async Task GetDocumentDetails(string documentId)
        {
            _showDocumentDetails = false;

            var documentMetadata = await Mediator.Send(new GetDocumentMetadataQuery(_indexItem.IndexName, documentId));
            var wrapper = new DocumentMetadataWrapper(new DocumentMetadata[] { documentMetadata },
                _indexItem.FieldMapping, _searchState.KeyField);
            _documentMetadata = wrapper.Documents().FirstOrDefault();
            _showDocumentDetails = true;
            StateHasChanged();

            //var stateModel = new StateModel(HASH_DOCUMENTPREVIEW, documentId);
            //NavManager.NavigateTo($"/search/{Index}/{StateService.GenerateHash(stateModel)}", forceLoad: false);
        }

        private async Task TimeSpanAnyOnClick(MouseEventArgs mouseEventArgs)
        {
            _timeSpanSelectedType = TimeSpanType.Any;
            UpdateTimeSpanMenu();
            await UpdateDateTimeFilter("datetime", null, null);
        }

        private async Task TimeSpanTodayOnClick(MouseEventArgs mouseEventArgs)
        {
            _timeSpanSelectedType = TimeSpanType.Today;
            UpdateTimeSpanMenu();
            await UpdateDateTimeFilter("datetime", DateTime.Now);
        }

        private DateTime GetMondayOfCurrentWeek()
        {
            var today = DateTime.Today;
            var daysToSubtract = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7;

            return today
                .AddDays(-daysToSubtract)
                .Date;
        }

        private async Task TimeSpanThisWeekOnClick(MouseEventArgs mouseEventArgs)
        {
            _timeSpanSelectedType = TimeSpanType.Week;
            UpdateTimeSpanMenu();
            await UpdateDateTimeFilter("datetime", GetMondayOfCurrentWeek(), DateTime.Today);
        }

        private DateTime GetFirstOfCurrentMonth()
        {
            var today = DateTime.Today;
            return new DateTime(today.Year, today.Month, 1);
        }

        private async Task TimeSpanThisMonthOnClick(MouseEventArgs mouseEventArgs)
        {
            _timeSpanSelectedType = TimeSpanType.Month;
            UpdateTimeSpanMenu();
            await UpdateDateTimeFilter("datetime", GetFirstOfCurrentMonth(), DateTime.Today);
        }

        private async Task TimeSpanCustomOnClick(MouseEventArgs mouseEventArgs)
        {
            _timeSpanSelectedType = TimeSpanType.Custom;
            if (_timespanPicker != null)
            {
                _timespanPicker.Open();
            }
        }

        private string DateTimeStandardFormat(DateTime d)
        {
            return d.ToString("yyyy-MM-dd");
        }

        private void UpdateTimeSpanMenu()
        {
            switch (_timeSpanSelectedType)
            {
                case TimeSpanType.Today:
                    _timespanLabel = $"{DateTimeStandardFormat(DateTime.Today)}";
                    break;
                case TimeSpanType.Week:
                    _timespanLabel = $"{DateTimeStandardFormat(GetMondayOfCurrentWeek())} - {DateTimeStandardFormat(DateTime.Today)}";
                    break;
                case TimeSpanType.Month:
                    _timespanLabel = $"{DateTime.Today.ToString("MMMM yyyy")}";
                    break;
                case TimeSpanType.Custom:
                    var start = _timespanPicker?.DateRange.Start?.ToString("yyyy-MM-dd");
                    var end = _timespanPicker?.DateRange.End?.ToString("yyyy-MM-dd");
                    _timespanLabel = $"{start} - {end}";
                    break;
                default:
                    if (current_lang.Equals("en"))
                        _timespanLabel = "Any";
                    else
                        _timespanLabel = "N'importe quel";
                    break;
            }
        }

        private async Task TimeSpanCustomRangeChanged(DateRange dateRange)
        {
            UpdateTimeSpanMenu();
            await UpdateDateTimeFilter("datetime", dateRange.Start, dateRange.End);
        }

        private string GetDateTimeSearchFormated(DateTime? dateTime)
        {
            if (dateTime is null)
                return string.Empty;

            return $"{dateTime?.ToString("yyyy-MM-dd")}T00:00:00Z";
        }

        private string? GetDateTimeSearchQuery(string fieldName, DateTime? a, DateTime? b = null)
        {
            var startDate = GetDateTimeSearchFormated(a);
            var endDate = GetDateTimeSearchFormated(b);

            if (a != null && b != null)
            {
                return $"le {endDate} and datetime ge {startDate}";
            }
            else if (a != null)
            {
                return $"ge {startDate}";
            }
            else if (b != null)
            {
                return $"le {endDate}";
            }

            return null;
        }

        private async Task UpdateDateTimeFilter(string fieldName, DateTime? a, DateTime? b = null)
        {
            FacetFilter? searchFacet = _selectedFilters.FirstOrDefault(x => x.Name != null && x.Name.Equals("datetime"));
            if (searchFacet == null)
            {
                searchFacet = new FacetFilter { Name = "datetime" };
                _selectedFilters.Add(searchFacet);
            }

            var searchQuery = GetDateTimeSearchQuery(fieldName, a, b);

            if (string.IsNullOrEmpty(searchQuery))
            {
                _selectedFilters.Remove(searchFacet);
            }
            else
            {
                searchFacet.Values = new string[]
                {
                searchQuery
                };
            }

            var request = new SearchDocumentsQuery(_indexItem, SearchText, _selectedPage, poligonString, _selectedFacets, _selectedFilters, _orderBy);

            await BeginSearchDocuments(request);
        }

        private async Task ClearSearch(MouseEventArgs args)
        {
            NavManager.NavigateTo($"/{Index}/search", forceLoad: false);
        }

        private async Task OrderByCallback(FacetFilter? facet)
        {
            if (facet != null)
            {
                _orderBy.RemoveAll(x => x.Name.Equals(facet.Name));
                _orderBy.Add(facet);
                await BeginSearchDocuments(new SearchDocumentsQuery(_indexItem, SearchText, _selectedPage, poligonString, _selectedFacets, _selectedFilters, _orderBy));
            }
        }

        private bool DisableClearFilter()
        {
            int? filtersCount = _selectedFacets?.Count + _selectedFilters?.Count;

            if (_orderBy.Count > 1)
            {
                filtersCount += _orderBy.Count;
            }
            else if (_orderBy.Count == 1)
            {
                var order = _orderBy.First();
                var defaultOrder = OrderByFacetFilter.BestMatch();
                if (!order.Equals(defaultOrder))
                    filtersCount += 1;
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
                filtersCount += 1;

            return filtersCount == 0;
        }

        private static List<FacetFilter> DefaultSortByFilters()
        {
            return new List<FacetFilter>() { OrderByFacetFilter.BestMatch() };
        }

        private async Task SortBestMatchClicked(MouseEventArgs args)
        {
            _sortLabel = LABEL_ORDERBY_BEST_MATCH;
            await OrderByCallback(OrderByFacetFilter.BestMatch());
        }

        private async Task SortDateClicked(MouseEventArgs args)
        {
            _sortLabel = LABEL_ORDERBY_DATE;
            await OrderByCallback(OrderByFacetFilter.Date());
        }
    }
}
