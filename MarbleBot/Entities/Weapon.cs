namespace MarbleBot.Entities;

public class Weapon : Item
{
    public int Accuracy { get; set; }
    public int Damage { get; set; }
    public int Hits { get; set; }
    public WeaponClass WeaponClass { get; set; }

    public List<WeaponAmmo> CompatibleAmmo { get; set; } = [];
}
