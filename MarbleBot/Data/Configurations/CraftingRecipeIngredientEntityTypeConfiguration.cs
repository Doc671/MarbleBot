using MarbleBot.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarbleBot.Data.Configurations;

public class CraftingRecipeIngredientEntityTypeConfiguration : IEntityTypeConfiguration<CraftingRecipeIngredient>
{
    public void Configure(EntityTypeBuilder<CraftingRecipeIngredient> builder)
    {
        builder.HasKey(recipeIngredient => new { recipeIngredient.TargetItemId, recipeIngredient.IngredientItemId });

        builder.HasOne(recipeIngredient => recipeIngredient.TargetItem)
            .WithMany(item => item.CraftingRecipeIngredientsAsTarget)
            .HasForeignKey(recipeIngredient => recipeIngredient.TargetItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(recipeIngredient => recipeIngredient.IngredientItem)
            .WithMany(item => item.CraftingRecipeIngredientsAsIngredient)
            .HasForeignKey(recipeIngredient => recipeIngredient.IngredientItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
