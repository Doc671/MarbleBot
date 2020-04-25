using MarbleBot.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace MarbleBot.Common
{
    [JsonConverter(typeof(ItemConverter))]
    public class Item
    {
        public int Id { get; }
        public string Name { get; }
        public decimal Price { get; }
        public string Description { get; }
        public bool OnSale { get; }
        public int Stage { get; }
        public ScavengeLocation ScavengeLocation { get; }
        public int CraftingProduced { get; }
        public Dictionary<int, int>? CraftingRecipe { get; }
        public int CraftingStationRequired { get; }

        [JsonConstructor]
        public Item(int? id = -1, string? name = "", decimal? price = 0m, string? description = "", bool? onSale = false,
            int? stage = 1, ScavengeLocation? scavengeLocation = ScavengeLocation.None, int? craftingProduced = 0,
            Dictionary<int, int>? craftingRecipe = null, int? craftingStationRequired = 0)
        {
            Id = id ?? -1;
            Name = name ?? "";
            Price = price ?? 0m;
            Description = description ?? "";
            OnSale = onSale ?? false;
            Stage = stage ?? 1;
            ScavengeLocation = scavengeLocation ?? ScavengeLocation.None;
            CraftingProduced = craftingProduced ?? 0;
            CraftingRecipe = craftingRecipe;
            CraftingStationRequired = craftingStationRequired ?? 0;
        }

        public Item(Item baseItem, int id = 0, bool onSale = false)
        {
            Id = id == 0 ? baseItem.Id : id;
            Name = baseItem.Name;
            Price = baseItem.Price;
            Description = baseItem.Description;
            OnSale = onSale == baseItem.OnSale ? onSale : baseItem.OnSale;
            Stage = baseItem.Stage;
            ScavengeLocation = baseItem.ScavengeLocation;
            CraftingProduced = baseItem.CraftingProduced;
            CraftingRecipe = baseItem.CraftingRecipe;
            CraftingStationRequired = baseItem.CraftingStationRequired;
        }

        public static T Find<T>(int itemId) where T : Item
        {
            var itemsDict = GetItems();
            if (itemsDict.ContainsKey(itemId))
            {
                return (T)itemsDict[itemId];
            }
            else
            {
                throw new Exception("The requested item could not be found.");
            }
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
                else
                {
                    throw new Exception("The requested item could not be found.");
                }
            }
            else
            {
                var itemsDict = GetItems();
                string newSearchTerm = searchTerm.ToLower().RemoveChar(' ');
                foreach (var itemPair in itemsDict)
                {
                    if (itemPair.Value.Name.ToLower().Contains(newSearchTerm) || newSearchTerm.Contains(itemPair.Value.Name.ToLower()))
                    {
                        return (T)itemPair.Value;
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
            return JsonConvert.DeserializeObject<IDictionary<int, Item>>(json);
        }

        public override string ToString() => $"`[{Id:000}]` **{Name}**";
    }
}
