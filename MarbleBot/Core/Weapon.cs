using Newtonsoft.Json;
using System.Collections.Generic;

namespace MarbleBot.Core
{
    /// <summary> Represents a weapon that can be used during a Siege or War. </summary> 
    public class Weapon : Item
    {
        /// <summary> The damage dealt by the weapon. </summary>
        public int Accuracy { get; }
        /// <summary> The ID of the ammo used by the ranged weapon. </summary>
        public int[] Ammo { get; }
        /// <summary> The damage dealt by the weapon. </summary>
        public int Damage { get; }
        /// <summmary> The number of times the weapon attacks. </summmary>
        public int Uses { get; }
        /// <summary> The class of the weapon. </summary>
        public WeaponClass WarClass { get; }

        [JsonConstructor]
        public Weapon(int id = 0, string name = "", decimal price = 0m, string description = "", bool onSale = false,
                      int stage = 1, ScavengeLocation scavengeLocation = ScavengeLocation.None,
                      uint craftingProduced = 0u, Dictionary<string, int> craftingRecipe = null,
                      int craftingStationRequired = 0, int accuracy = 0, int[] ammo = null, int damage = 0, int uses = 1,
                      WeaponClass warClass = WeaponClass.None) : base(id, name, price, description, onSale, stage,
                                                                      scavengeLocation, craftingProduced, craftingRecipe,
                                                                      craftingStationRequired)
        {
            Accuracy = accuracy;
            Ammo = ammo;
            Damage = damage;
            Uses = uses;
            WarClass = warClass;
        }
    }
}
