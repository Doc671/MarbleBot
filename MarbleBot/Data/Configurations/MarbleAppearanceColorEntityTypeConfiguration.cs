using MarbleBot.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarbleBot.Data.Configurations;

public class MarbleAppearanceColorEntityTypeConfiguration : IEntityTypeConfiguration<MarbleAppearanceColor>
{
    public void Configure(EntityTypeBuilder<MarbleAppearanceColor> builder)
    {
        builder.HasOne(color => color.Appearance)
            .WithMany(appearance => appearance.Colors)
            .HasForeignKey(color => color.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prevents two colours in the same user's list from having the same order in the list
        builder.HasIndex(color => new { color.UserId, color.Order })
            .IsUnique();
    }
}
