using System;

namespace MarbleBot.Common.Games.Siege
{
    public readonly struct BossDropInfo : IEquatable<BossDropInfo>
    {
        public int ItemId { get; }
        public int MinCount { get; }
        public int MaxCount { get; }
        public int Chance { get; }

        public BossDropInfo(int itemId, int minCount, int maxCount, int chance)
        {
            ItemId = itemId;
            MinCount = minCount;
            MaxCount = maxCount;
            Chance = chance;
        }

        public override bool Equals(object? obj)
        {
            return obj is BossDropInfo drops &&
                   ItemId == drops.ItemId &&
                   MinCount == drops.MinCount &&
                   MaxCount == drops.MaxCount &&
                   Chance == drops.Chance;
        }

        public bool Equals(BossDropInfo other)
        {
            return ItemId == other.ItemId &&
                   MinCount == other.MinCount &&
                   MaxCount == other.MaxCount &&
                   Chance == other.Chance;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ItemId, MinCount, MaxCount, Chance);
        }

        public static bool operator ==(BossDropInfo left, BossDropInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BossDropInfo left, BossDropInfo right)
        {
            return !(left == right);
        }
    }
}
