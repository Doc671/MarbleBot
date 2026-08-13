using MarbleBot.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarbleBot.Data.Configurations;

public class WeaponAmmoEntityTypeConfiguration : IEntityTypeConfiguration<WeaponAmmo>
{
    public void Configure(EntityTypeBuilder<WeaponAmmo> builder)
    {
        builder.HasKey(weaponAmmo => new { weaponAmmo.WeaponId, weaponAmmo.AmmoId });

        builder.HasOne(weaponAmmo => weaponAmmo.Weapon)
            .WithMany(weapon => weapon.CompatibleAmmo)
            .HasForeignKey(weaponAmmo => weaponAmmo.WeaponId);

        builder.HasOne(weaponAmmo => weaponAmmo.Ammo)
            .WithMany(ammo => ammo.CompatibleWeapons)
            .HasForeignKey(weaponAmmo => weaponAmmo.AmmoId);
    }
}
