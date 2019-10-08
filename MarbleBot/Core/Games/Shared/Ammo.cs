using Newtonsoft.Json;
using System.Collections.Generic;

namespace MarbleBot.Core
{
    /// <summary> Represents ammunition for ranged weapons used during Sieges and Wars. </summary> 
    public class Ammo : Item
    {
        /// <summary> The damage dealt by the ammo. </summary>
        public int Damage { get; }

        [JsonConstructor]
        public Ammo(uint id = 0, string name = "", decimal price = 0m, string description = "", bool onSale = false,
                      int stage = 1, ScavengeLocation scavengeLocation = ScavengeLocation.None,
                      uint craftingProduced = 0u, Dictionary<string, int> craftingRecipe = null,
                      int craftingStationRequired = 0, int damage = 0) : base(id, name, price, description, onSale, stage,
                                                                      scavengeLocation, craftingProduced, craftingRecipe,
                                                                      craftingStationRequired)
        {
            Damage = damage;
        }
    }
}
