using MarbleBot.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarbleBot.Data.Configurations;

public class MarbleAppearanceEntityTypeConfiguration : IEntityTypeConfiguration<MarbleAppearance>
{
    public void Configure(EntityTypeBuilder<MarbleAppearance> builder)
    {
        builder.HasKey(appearance => appearance.UserId);

        builder.HasOne(appearance => appearance.User)
            .WithOne()
            .HasForeignKey<MarbleAppearance>(appearance => appearance.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
