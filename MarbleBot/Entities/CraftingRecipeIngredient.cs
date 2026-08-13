namespace MarbleBot.Entities;

public class CraftingRecipeIngredient
{
    public int TargetItemId { get; init; }
    public Item TargetItem { get; init; } = null!;

    public int IngredientItemId { get; init; }
    public Item IngredientItem { get; init; } = null!;

    public int Quantity { get; init; }
}
