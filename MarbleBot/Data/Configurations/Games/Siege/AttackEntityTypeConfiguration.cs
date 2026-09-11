using MarbleBot.Entities.Games.Siege;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarbleBot.Data.Configurations.Games.Siege;

public class AttackEntityTypeConfiguration : IEntityTypeConfiguration<Attack>
{
    public void Configure(EntityTypeBuilder<Attack> builder)
    {
        builder.HasKey(attack => attack.Id);

        builder.HasIndex(attack => attack.Name);
        builder.Property(attack => attack.Name).IsRequired().HasMaxLength(64);

        builder.HasOne(attack => attack.Boss)
            .WithMany(boss => boss.Attacks)
            .HasForeignKey(attack => attack.BossId);
    }
}
