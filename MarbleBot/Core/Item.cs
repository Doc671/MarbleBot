using System.Collections.Generic;

namespace MarbleBot.Core
{
    /// <summary> Represents an inventory item. </summary> 
    public struct Item
    {
        /// <summary> The identification number of the item. </summary>
        public int Id { get; set; }
        /// <summary> The name of the item. </summary>
        public string Name { get; set; }
        /// <summary> The price of the item. </summary>
        public decimal Price { get; set; }
        /// <summary> The description of the item. </summary>
        public string Description { get; set; }
        /// <summary> Whether the item is available to buy in the shop or not. </summary>
        public bool OnSale { get; set; }
        /// <summary> The Stage at which the item can be obtained. </summary>
        public byte Stage { get; set; }
        /// <summary> The location the item can be found in during a Scavenge game. </summary>
        public ScavengeLocation ScavengeLocation { get; set; }
        /// <summary> The class of the weapon during a War game. </summary>
        public WarClass WarClass { get; set; }
        /// <summary> The quantity produced of this item upon being crafted. </summary>
        public uint CraftingProduced { get; set; }
        /// <summary> The crafting recipe - a key value pair of item IDs and their necessary quantities. </summary>
        public Dictionary<string, int> CraftingRecipe { get; set; }
        /// <summary> Which crafting station is required to craft the item. </summary>
        public byte CraftingStationRequired { get; set; }

        /// <summary> Converts this item into a string representation. </summary>
        public override string ToString()
            => $"`[{Id.ToString("000")}]` **{Name}**";
    }
}