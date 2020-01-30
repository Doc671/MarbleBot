using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MarbleBot.Common
{
    public class Weapon : Item
    {
        public int Accuracy { get; }
        public ImmutableArray<int> Ammo { get; }
        public int Damage { get; }
        public int Hits { get; }
        public WeaponClass WarClass { get; }

        [JsonConstructor]
        public Weapon(int id = 0, string name = "", decimal price = 0m, string description = "", bool onSale = false,
                      int stage = 1, ScavengeLocation scavengeLocation = ScavengeLocation.None,
                      int craftingProduced = 0, Dictionary<string, int>? craftingRecipe = null,
                      int craftingStationRequired = 0, int accuracy = 0, int[]? ammo = null, int damage = 0, int hits = 1,
                      WeaponClass warClass = WeaponClass.None) : base(id, name, price, description, onSale, stage,
                                                                      scavengeLocation, craftingProduced, craftingRecipe,
                                                                      craftingStationRequired)
        {
            Accuracy = accuracy;
            Ammo = ImmutableArray.Create(ammo);
            Damage = damage;
            Hits = hits;
            WarClass = warClass;
        }
    }
}
