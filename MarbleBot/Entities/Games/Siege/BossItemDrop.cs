namespace MarbleBot.Entities.Games.Siege;

public class BossItemDrop
{
    public ulong BossId { get; init; }
    public Boss Boss { get; init; } = null!;

    public int ItemId { get; init; }
    public Item Item { get; init; } = null!;

    public int MinCount { get; init; }
    public int MaxCount { get; init; }
    public int Chance { get; init; }
}
