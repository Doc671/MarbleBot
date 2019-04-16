using System.Collections.Generic;

namespace MarbleBot.BaseClasses
{
    /// <summary> Class for items when using money-based commands </summary> 
    public struct Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public bool OnSale { get; set; }
        public byte Stage { get; set; }
        public ScavengeLocation ScavengeLocation { get; set; }
        public uint CraftingProduced { get; set; }
        public Dictionary<string, int> CraftingRecipe { get; set; }
        public byte CraftingStationRequired { get; set; }

        public override string ToString()
            => $"[{Id}] {Name}";
    }
}