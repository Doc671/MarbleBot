namespace MarbleBot.Common
{
    public readonly struct BossDrops
    {
        public int ItemId { get; }
        public int MinCount { get; }
        public int MaxCount { get; }
        public int Chance { get; }

        public BossDrops(int itemId, int minCount, int maxCount, int chance)
        {
            ItemId = itemId;
            MinCount = minCount;
            MaxCount = maxCount;
            Chance = chance;
        }

        public override bool Equals(object? obj)
        {
            return obj is BossDrops drops &&
                   ItemId == drops.ItemId &&
                   MinCount == drops.MinCount &&
                   MaxCount == drops.MaxCount &&
                   Chance == drops.Chance;
        }

        public override int GetHashCode()
        {
            return 17 * (ItemId + MinCount + MaxCount + Chance);
        }

        public static bool operator ==(BossDrops left, BossDrops right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BossDrops left, BossDrops right)
        {
            return !(left == right);
        }
    }
}
