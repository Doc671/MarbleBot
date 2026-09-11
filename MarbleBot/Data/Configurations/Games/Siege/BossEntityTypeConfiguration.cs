using MarbleBot.Entities.Games.Siege;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarbleBot.Data.Configurations.Games.Siege;

public class BossEntityTypeConfiguration : IEntityTypeConfiguration<Boss>
{
    public void Configure(EntityTypeBuilder<Boss> builder)
    {
        builder.HasKey(boss => boss.Id);

        builder.HasIndex(boss => boss.Name).IsUnique();
        builder.Property(boss => boss.Name).IsRequired().HasMaxLength(64);
    }
}
