using System.Collections.Generic;

namespace MarbleBot.Common
{
    public class Shield : Item
    {
        public int DamageAbsorption { get; }

        public Shield(int? id = 0, string? name = "", decimal? price = 0, string? description = "", bool? onSale = false,
                      int? stage = 1, ScavengeLocation? scavengeLocation = ScavengeLocation.None, int? craftingProduced = 0,
                      Dictionary<int, int>? craftingRecipe = null, int? craftingStationRequired = 0, int? damageAbsorption = 0)
            : base(id, name, price, description, onSale, stage, scavengeLocation, craftingProduced, craftingRecipe, craftingStationRequired)
        {
            DamageAbsorption = damageAbsorption ?? 0;
        }
    }
}
