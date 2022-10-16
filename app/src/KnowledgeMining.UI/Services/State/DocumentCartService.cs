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

        public async Task<bool> Add(string index, string title, string recordId)
        {
            var key = Key(index);
            var items = await GetAll(index);
            if (items == null)
            {
                items = new List<DocumentCartItem>();
            }
            
            if (!items.Any(x => x?.RecordId?.Equals(recordId) ?? false))
            {
                var item = new DocumentCartItem { RecordId = recordId, Title = title };
                items.Add(item);
                await _localStorageService.SetItemAsync<IList<DocumentCartItem>>(key, items);
                await Notify(new DocumentCartEvent(CartAction.Add, item, items));
                return true;
            }

            return false;
        }

        public async Task<IList<DocumentCartItem>> GetAll(string index)
        {
            var key = Key(index);
            var items = await _localStorageService.GetItemAsync<IList<DocumentCartItem>>(key);
            return items ?? new List<DocumentCartItem>();
        }

        public async Task<DocumentCartItem?> Remove(string index, string recordId)
        {
            var key = Key(index);
            var items = await GetAll(index);
            if(items != null)
            {
                var item = items.FirstOrDefault(x => x?.RecordId?.Equals(recordId) ?? false);
                if (item != null)
                {
                    items.Remove(item);
                    await _localStorageService.SetItemAsync<IList<DocumentCartItem>>(key, items);
                    await Notify(new DocumentCartEvent(CartAction.Add, item, items));
                }

                return item;
            }

            return null;
        }

        public async Task Clear(string index)
        {
            var key = Key(index);
            await _localStorageService.RemoveItemAsync(key);
            await Notify(new DocumentCartEvent(CartAction.Clear, null, new List<DocumentCartItem>()));
        }
    }
}
