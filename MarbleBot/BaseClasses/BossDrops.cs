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
        /// <param name="itemId">The ID of the item that is dropped.</param>
        /// <param name="minCount">The minimum possible number of this item that the boss can drop.</param>
        /// <param name="maxCount">The maximum possible number of this item that the boss can drop.</param>
        /// <param name="chance">The chance as a percentage that these items will drop.</param>
        public BossDrops(int itemId, ushort minCount, ushort maxCount, byte chance) {
            ItemId = itemId;
            MinCount = minCount;
            MaxCount = maxCount;
            Chance = chance;
        }
    }
}
