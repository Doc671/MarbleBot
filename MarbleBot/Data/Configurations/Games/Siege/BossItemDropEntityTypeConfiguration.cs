using MarbleBot.Entities.Games.Siege;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarbleBot.Data.Configurations.Games.Siege;

public class BossItemDropEntityTypeConfiguration : IEntityTypeConfiguration<BossItemDrop>
{
    public void Configure(EntityTypeBuilder<BossItemDrop> builder)
    {
        builder.HasKey(bossItemDrop => new { bossItemDrop.BossId, bossItemDrop.ItemId });

        builder.HasOne(bossItemDrop => bossItemDrop.Boss)
            .WithMany(boss => boss.Drops)
            .HasForeignKey(bossItemDrop => bossItemDrop.BossId);
    }
}
