namespace MarbleBot.Entities;

public class LegacyItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public bool OnSale { get; init; }
    public int Stage { get; init; }
    public int ScavengeLocation { get; init; }
    public int CraftingProduced { get; init; }
    public Dictionary<int, int>? CraftingRecipe { get; init; }
    public int CraftingStationRequired { get; init; }

    public int? Accuracy { get; init; }
    public int? Damage { get; init; }
    public int? Hits { get; init; }
    public WeaponClass? WeaponClass { get; init; }

    public float? IncomingDamageMultiplier { get; init; }
    public float? OutgoingDamageMultiplier { get; init; }
}
