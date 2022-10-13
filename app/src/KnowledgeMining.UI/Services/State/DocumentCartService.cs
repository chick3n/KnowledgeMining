using Blazored.LocalStorage;

namespace KnowledgeMining.UI.Services.State
{
    public class DocumentCartService
    {
        private readonly ILocalStorageService _localStorageService;
        private readonly ILogger _logger;

        private const string KEY_SUFFIX = "-documents";

        public Func<DocumentCartEvent, Task> OnCartChanged { get; set; }

        public DocumentCartService(ILocalStorageService localStorageService,
            ILogger<DocumentCartService> logger)
        {
            _localStorageService = localStorageService;
            _logger = logger;
        }

        private string Key(string index)
        {
            return $"{index}{KEY_SUFFIX}";
        }

        private async Task Notify(DocumentCartEvent documentCartEvent)
        {
            if(OnCartChanged != null)
                await OnCartChanged.Invoke(documentCartEvent);
        }

        public async Task<bool> Add(string index, string value)
        {
            var key = Key(index);
            var items = await GetAll(index);
            if (items == null)
            {
                items = new List<string>();
            }
            
            if (!items.Contains(value))
            {
                items.Add(value);
                await _localStorageService.SetItemAsync<IList<string>>(key, items);
                await Notify(new DocumentCartEvent(CartAction.Add, value, items));
                return true;
            }

            return false;
        }

        public async Task<IList<string>> GetAll(string index)
        {
            var key = Key(index);
            var items = await _localStorageService.GetItemAsync<IList<string>>(key);
            return items;
        }
    }
}
