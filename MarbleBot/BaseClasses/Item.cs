﻿namespace MarbleBot.BaseClasses
{
    /// <summary> Class for items when using money-based commands </summary> 
    public struct Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public bool OnSale { get; set; }
        public bool DiveCollectable { get; set; }
    }
}