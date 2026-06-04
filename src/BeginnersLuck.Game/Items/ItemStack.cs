namespace BeginnersLuck.Game.Items;

public sealed record ItemStack
{
    public string ItemId { get; }

    public int Quantity { get; }

    public ItemStack(string itemId, int quantity)
    {
        ItemId = itemId ?? string.Empty;
        Quantity = quantity < 0 ? 0 : quantity;
    }
}
