using MarbleBot.Common.Games.Scavenge;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MarbleBot.Common.Games
{
    public class Weapon : Item
    {
        [JsonConstructor]
        public Weapon(int? id = 0, string? name = "", decimal? price = 0m, string? description = "",
            bool? onSale = false,
            int? stage = 1, ScavengeLocation? scavengeLocation = ScavengeLocation.None,
            int? craftingProduced = 0, Dictionary<int, int>? craftingRecipe = null,
            int? craftingStationRequired = 0, int? accuracy = 0, int[]? ammo = null, int? damage = 0, int? hits = 1,
            WeaponClass? weaponClass = WeaponClass.None) : base(id, name, price, description, onSale, stage,
            scavengeLocation, craftingProduced, craftingRecipe,
            craftingStationRequired)
        {
            Accuracy = accuracy ?? 0;
            Ammo = ImmutableArray.Create(ammo);
            Damage = damage ?? 0;
            Hits = hits ?? 0;
            WeaponClass = weaponClass ?? WeaponClass.None;
        }

        public int Accuracy { get; }
        public ImmutableArray<int> Ammo { get; }
        public int Damage { get; }
        public int Hits { get; }
        public WeaponClass WeaponClass { get; }
    }
}
