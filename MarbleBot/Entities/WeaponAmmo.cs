namespace MarbleBot.Entities;

public class WeaponAmmo
{
    public int WeaponId { get; set; }
    public Weapon Weapon { get; set; } = null!;

    public int AmmoId { get; set; }
    public Ammo Ammo { get; set; } = null!;
}
