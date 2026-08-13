namespace MarbleBot.Entities;

public class UserInventoryItem
{
    public ulong UserId { get; set; }
    public User User { get; set; } = null!;

    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public int Quantity { get; set; }

    public void Deconstruct(out Item item, out int quantity)
    {
        item = Item;
        quantity = Quantity;
    }
}
