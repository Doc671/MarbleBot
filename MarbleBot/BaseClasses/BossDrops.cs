namespace MarbleBot.BaseClasses
{
    /// <summary> Object containing information about an item a boss drops. </summary>
    public readonly struct BossDrops
    {
        /// <summary> The ID of the item that is dropped. </summary>
        public int ItemId { get; }
        /// <summary> The minimum possible number of this item that the boss can drop. </summary>
        public ushort MinCount { get; }
        /// <summary> The maximum possible number of this item that the boss can drop. </summary>
        public ushort MaxCount { get; }
        /// <summary> The chance as a percentage that these items will drop. </summary>
        public byte Chance { get; }

        /// <summary> Object containing information about an item a boss drops. </summary>
        /// <param name="id">The ID of the item that is dropped.</param>
        /// <param name="minDrops">The minimum possible number of this item that the boss can drop.</param>
        /// <param name="maxDrops">The maximum possible number of this item that the boss can drop.</param>
        /// <param name="chance">The chance as a percentage that these items will drop.</param>
        public BossDrops(int id, ushort minDrops, ushort maxDrops, byte chance) {
            ItemId = id;
            MinCount = minDrops;
            MaxCount = maxDrops;
            Chance = chance;
        }
    }
}
