using MarbleBot.Common.Games.Scavenge;
using System.Collections.Generic;

namespace MarbleBot.Common.Games
{
    public class Spikes : Item
    {
        public Spikes(int? id = 0, string? name = "", decimal? price = 0, string? description = "",
            bool? onSale = false, int? stage = 1, ScavengeLocation? scavengeLocation = ScavengeLocation.None,
            int? craftingProduced = 0, Dictionary<int, int>? craftingRecipe = null, int? craftingStationRequired = 0,
            float? outgoingDamageMultiplier = 0)
            : base(id, name, price, description, onSale, stage, scavengeLocation, craftingProduced, craftingRecipe,
                craftingStationRequired)
        {
            OutgoingDamageMultiplier = outgoingDamageMultiplier ?? 0;
        }

        public float OutgoingDamageMultiplier { get; }
    }
}
