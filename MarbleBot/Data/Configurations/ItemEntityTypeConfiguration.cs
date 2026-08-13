using MarbleBot.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarbleBot.Data.Configurations;

public class ItemEntityTypeConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();

        builder.HasIndex(item => item.Name);
        builder.Property(item => item.Name).IsRequired().HasMaxLength(200);

        builder.Property(item => item.Value).HasColumnType("numeric(20,2)");

        builder.HasDiscriminator<string>("ItemType")
            .HasValue<Item>("Item")
            .HasValue<Ammo>("Ammo")
            .HasValue<Shield>("Shield")
            .HasValue<Spikes>("Spikes")
            .HasValue<Weapon>("Weapon");
    }
}
