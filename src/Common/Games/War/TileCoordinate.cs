using System;

namespace MarbleBot.Common.Games.War
{
    public readonly struct TileCoordinate
    {
        public int X { get; }
        public int Y { get; }

        public TileCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object? obj)
        {
            return obj is TileCoordinate coordinate &&
                   X == coordinate.X &&
                   Y == coordinate.Y;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }
    }
}
