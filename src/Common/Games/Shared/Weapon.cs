using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MarbleBot.Common
{
    /// <summary> Represents a weapon that can be used during a Siege or War. </summary> 
    public class Weapon : Item
    {
        /// <summary> The damage dealt by the weapon. </summary>
        public int Accuracy { get; }
        /// <summary> The ID of the ammo used by the ranged weapon. </summary>
        public ImmutableArray<uint> Ammo { get; }
        /// <summary> The damage dealt by the weapon. </summary>
        public int Damage { get; }
        /// <summmary> The number of times the weapon attacks. </summmary>
        public int Hits { get; }
        /// <summary> The class of the weapon. </summary>
        public WeaponClass WarClass { get; }

        [JsonConstructor]
        public Weapon(uint id = 0, string name = "", decimal price = 0m, string description = "", bool onSale = false,
                      int stage = 1, ScavengeLocation scavengeLocation = ScavengeLocation.None,
                      uint craftingProduced = 0u, Dictionary<string, int> craftingRecipe = null,
                      int craftingStationRequired = 0, int accuracy = 0, uint[] ammo = null, int damage = 0, int hits = 1,
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
