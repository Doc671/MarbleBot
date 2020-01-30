using Newtonsoft.Json;
using System.Collections.Generic;

namespace MarbleBot.Common
{
    public class Item
    {
        public int Id { get; internal set; }
        public string Name { get; }
        public decimal Price { get; }
        public string Description { get; }
        public bool OnSale { get; }
        public int Stage { get; }
        public ScavengeLocation ScavengeLocation { get; }
        public int CraftingProduced { get; }
        public Dictionary<string, int>? CraftingRecipe { get; }
        public int CraftingStationRequired { get; }

        [JsonConstructor]
        public Item(int id = 0, string name = "", decimal price = 0m, string description = "", bool onSale = false,
            int stage = 1, ScavengeLocation scavengeLocation = ScavengeLocation.None, int craftingProduced = 0,
            Dictionary<string, int>? craftingRecipe = null, int craftingStationRequired = 0)
        {
            Id = id;
            Name = name;
            Price = price;
            Description = description;
            OnSale = onSale;
            Stage = stage;
            ScavengeLocation = scavengeLocation;
            CraftingProduced = craftingProduced;
            CraftingRecipe = craftingRecipe ?? new Dictionary<string, int>();
            CraftingStationRequired = craftingStationRequired;
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

        public override string ToString()
    => $"`[{Id.ToString("000")}]` **{Name}**";
    }
}
