namespace KnowledgeMining.UI.Services.State
{
    public record DocumentCartEvent(CartAction Action, string Item, IList<string> Items);
}
