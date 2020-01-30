using Newtonsoft.Json;
using System.Collections.Generic;

namespace MarbleBot.Common
{
    public class Ammo : Item
    {
        public int Damage { get; }

        [JsonConstructor]
        public Ammo(int id = 0, string name = "", decimal price = 0m, string description = "", bool onSale = false,
                      int stage = 1, ScavengeLocation scavengeLocation = ScavengeLocation.None,
                      int craftingProduced = 0, Dictionary<string, int>? craftingRecipe = null,
                      int craftingStationRequired = 0, int damage = 0) : base(id, name, price, description, onSale, stage,
                                                                      scavengeLocation, craftingProduced, craftingRecipe,
                                                                      craftingStationRequired)
        {
            Damage = damage;
        }
    }
}
