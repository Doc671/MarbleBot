namespace MarbleBot.Entities;

public class Ammo : Item
{
    public int Damage { get; set; }

    public List<WeaponAmmo> CompatibleWeapons { get; set; } = [];
}
