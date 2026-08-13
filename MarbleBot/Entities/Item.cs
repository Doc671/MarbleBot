namespace MarbleBot.Entities;

public class Item
{
    public int Id { get; init; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public decimal Value { get; set; }
    public int Stage { get; set; }
    public int ScavengeLocation { get; set; }
    public int CraftingProduced { get; set; }
    public int CraftingStationRequired { get; set; }

    public List<UserInventoryItem> Owners { get; set; } = null!;
    public List<CraftingRecipeIngredient> CraftingRecipeIngredientsAsTarget { get; set; } = null!;
    public List<CraftingRecipeIngredient> CraftingRecipeIngredientsAsIngredient { get; set; } = null!;
}
