using MarbleBot.Common.Games;
using MarbleBot.Common.Games.Scavenge;
using MarbleBot.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MarbleBot.Common
{
    [JsonConverter(typeof(ItemConverter))]
    public record Item(int Id, string Name, decimal Price, string Description, bool OnSale, int Stage,
        ScavengeLocation ScavengeLocation, int CraftingProduced, Dictionary<int, int>? CraftingRecipe, 
        int CraftingStationRequired)
    {
        public static T Find<T>(int itemId) where T : Item
        {
            var itemsDict = GetItems();
            if (itemsDict.ContainsKey(itemId))
            {
                return (T)itemsDict[itemId];
            }

            throw new Exception("The requested item could not be found.");
        }

        public static T Find<T>(string searchTerm) where T : Item
        {
            if (int.TryParse(searchTerm, out int itemId))
            {
                var itemsDict = GetItems();
                if (itemsDict.ContainsKey(itemId))
                {
                    return (T)itemsDict[itemId];
                }

                throw new Exception("The requested item could not be found.");
            }
            else
            {
                var itemsDict = GetItems();
                string newSearchTerm = searchTerm.ToLower().RemoveChar(' ');
                foreach ((int _, Item item) in itemsDict)
                {
                    if (item.Name.ToLower().Contains(newSearchTerm) ||
                        newSearchTerm.Contains(item.Name.ToLower()))
                    {
                        return (T)item;
                    }
                }

                throw new Exception("The requested item could not be found.");
            }
        }

        public static IDictionary<int, Item> GetItems()
        {
            string json;
            using (var itemFile = new StreamReader($"Resources{Path.DirectorySeparatorChar}Items.json"))
            {
                json = itemFile.ReadToEnd();
            }
            return JsonSerializer.Deserialize<IDictionary<string, Item>>(json)!
                    .ToDictionary(pair => int.Parse(pair.Key), pair => pair.Value);
        }

        public override string ToString()
        {
            return $"`[{Id:000}]` **{Name}**";
        }
    }

    public record Ammo(int Id, string Name, decimal Price, string Description, bool OnSale, int Stage,
        ScavengeLocation ScavengeLocation, int CraftingProduced, Dictionary<int, int>? CraftingRecipe, int CraftingStationRequired,
        int Damage) : Item(Id, Name, Price, Description, OnSale, Stage, ScavengeLocation, CraftingProduced, CraftingRecipe,
        CraftingStationRequired)
    {
        public override string ToString()
        {
            return base.ToString();
        }
    }

    public record Weapon(int Id, string Name, decimal Price, string Description, bool OnSale, int Stage,
        ScavengeLocation ScavengeLocation, int CraftingProduced, Dictionary<int, int>? CraftingRecipe, int CraftingStationRequired,
        int Accuracy, ImmutableArray<int> Ammo, int Damage, int Hits, WeaponClass WeaponClass) : Item(Id, Name, Price, Description,
        OnSale, Stage, ScavengeLocation, CraftingProduced, CraftingRecipe, CraftingStationRequired)
    {
        public override string ToString()
        {
            return base.ToString();
        }
    }

    public record Shield(int Id, string Name, decimal Price, string Description, bool OnSale, int Stage,
        ScavengeLocation ScavengeLocation, int CraftingProduced, Dictionary<int, int>? CraftingRecipe, int CraftingStationRequired,
        float IncomingDamageMultiplier) : Item(Id, Name, Price, Description, OnSale, Stage, ScavengeLocation, CraftingProduced, CraftingRecipe,
        CraftingStationRequired)
    {
        public override string ToString()
        {
            return base.ToString();
        }
    }

    public record Spikes(int Id, string Name, decimal Price, string Description, bool OnSale, int Stage,
        ScavengeLocation ScavengeLocation, int CraftingProduced, Dictionary<int, int>? CraftingRecipe, int CraftingStationRequired,
        float OutgoingDamageMultiplier) : Item(Id, Name, Price, Description, OnSale, Stage, ScavengeLocation, CraftingProduced, CraftingRecipe,
        CraftingStationRequired)
    {
        public override string ToString()
        {
            return base.ToString();
        }
    }
}
