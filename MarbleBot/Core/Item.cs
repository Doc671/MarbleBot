using Newtonsoft.Json;
using System.Collections.Generic;

namespace MarbleBot.Core
{
    /// <summary> Represents an inventory item. </summary> 
    public readonly struct Item
    {
        /// <summary> The identification number of the item. </summary>
        public int Id { get; }
        /// <summary> The name of the item. </summary>
        public string Name { get; }
        /// <summary> The price of the item. </summary>
        public decimal Price { get; }
        /// <summary> The description of the item. </summary>
        public string Description { get; }
        /// <summary> Whether the item is available to buy in the shop or not. </summary>
        public bool OnSale { get; }
        /// <summary> The Stage at which the item can be obtained. </summary>
        public byte Stage { get; }
        /// <summary> The ID of the ammo used by the ranged weapon. </summary>
        public int[] Ammo { get; }
        /// <summary> The damage dealt by the weapon during a War game. </summary>
        public byte Damage { get; }
        /// <summary> The location the item can be found in during a Scavenge game. </summary>
        public ScavengeLocation ScavengeLocation { get; }
        /// <summary> The class of the weapon during a War game. </summary>
        public WarClass WarClass { get; }
        /// <summary> The quantity produced of this item upon being crafted. </summary>
        public uint CraftingProduced { get; }
        /// <summary> The crafting recipe - a key value pair of item IDs and their necessary quantities. </summary>
        public Dictionary<string, int> CraftingRecipe { get; }
        /// <summary> Which crafting station is required to craft the item. </summary>
        public byte CraftingStationRequired { get; }

        [JsonConstructor]
        public Item(int id = 0, string name = "", decimal price = 0m, string description = "", bool onSale = false,
            byte stage = 1, int[] ammo = null, byte damage = 0, ScavengeLocation scavengeLocation = ScavengeLocation.None,
            WarClass warClass = WarClass.None, uint craftingProduced = 0u, Dictionary<string, int> craftingRecipe = null,
            byte craftingStationRequired = 0)
        {
            Id = id;
            Name = name;
            Price = price;
            Description = description;
            OnSale = onSale;
            Stage = stage;
            Ammo = ammo;
            Damage = damage;
            ScavengeLocation = scavengeLocation;
            WarClass = warClass;
            CraftingProduced = craftingProduced;
            CraftingRecipe = craftingRecipe;
            CraftingStationRequired = craftingStationRequired;
        }

        public Item(Item baseItem, int id = 0, bool onSale = false, byte stage = 1, Dictionary<string, int> craftingRecipe = null)
        {
            Id = id == 0 ? baseItem.Id : id;
            Name = baseItem.Name;
            Price = baseItem.Price;
            Description = baseItem.Description;
            OnSale = onSale == baseItem.OnSale ? onSale : baseItem.OnSale;
            Stage = stage == 1 && baseItem.Stage != 0 ? baseItem.Stage : stage;
            Ammo = baseItem.Ammo;
            Damage = baseItem.Damage;
            ScavengeLocation = baseItem.ScavengeLocation;
            WarClass = baseItem.WarClass;
            CraftingProduced = baseItem.CraftingProduced;
            CraftingRecipe = craftingRecipe ?? baseItem.CraftingRecipe;
            CraftingStationRequired = baseItem.CraftingStationRequired;
        }

        /// <summary> Converts this item into a string representation. </summary>
        public override string ToString()
            => $"`[{Id.ToString("000")}]` **{Name}**";
    }
}