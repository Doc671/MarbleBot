using MarbleBot.Services;
using System;
using System.Linq;
using System.Text;

namespace MarbleBot.Common.Games.War
{
    public class Grid
    {
        private readonly Tile[,] _tiles;
        private readonly int _width;
        private readonly int _height;

        public Grid(WarTeam leftTeam, WarTeam rightTeam, RandomService randomService)
        {
            int totalContestants = leftTeam.Marbles.Count + rightTeam.Marbles.Count;
            _width = 2 * totalContestants + 2;
            _height = 2 * totalContestants + 1;
            _tiles = new Tile[_width, _height];

            for (int i = 0; i < _width; i++)
            {
                for (int j = 0; j < _height; j++)
                {
                    _tiles[i, j] = new Tile();
                }
            }

            for (int teamIndex = 0; teamIndex < leftTeam.Marbles.Count; teamIndex++)
            {
                WarMarble leftMarble = leftTeam.Marbles.ElementAt(teamIndex);
                leftMarble.Position = new TileCoordinate(0, 2 + teamIndex * 2);
                _tiles[leftMarble.Position.X, leftMarble.Position.Y].Element = leftMarble;

                WarMarble rightMarble = rightTeam.Marbles.ElementAt(teamIndex);
                rightMarble.Position = new TileCoordinate(_width - 1, 2 + teamIndex * 2);
                _tiles[rightMarble.Position.X, rightMarble.Position.Y].Element = rightMarble;
            }

            for (int i = 0; i < totalContestants; i++)
            {
                int row, column;
                do
                {
                    row = randomService.Rand.Next(0, _width);
                    column = randomService.Rand.Next(0, _height);
                }
                while (_tiles[row, column].Element != null);

                _tiles[row, column].Element = new Rock()
                {
                    Position = new TileCoordinate(row, column)
                };
            }
        }

        public string Display()
        {
            var output = new StringBuilder();
            for (int y = 0; y < _height; y++)
            {
                output.AppendLine();
                for (int x = 0; x < _width; x++)
                {
                    output.Append(_tiles[x, y].DisplayEmoji);
                }
            }
            return output.ToString();
        }

        public bool IsPathClear(TileCoordinate userPosition, TileCoordinate targetPosition)
        {
            int changeX = Math.Sign(targetPosition.X - userPosition.X);
            int changeY = Math.Sign(targetPosition.Y - userPosition.Y);
            int currentX = userPosition.X;
            int currentY = userPosition.Y;
            while (currentX != targetPosition.X && currentY != targetPosition.Y)
            {
                currentX += changeX;
                if (_tiles[currentX, currentY].Element != null)
                {
                    return false;
                }

                currentY += changeY;
                if (_tiles[currentX, currentY].Element != null)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsValidCoords(int x, int y)
        {
            return x >= 0 && x < _width 
                  && y >= 0 && y < _height 
                  && _tiles[x, y].Element == null;
        }

        public static bool IsWithinDistance(WarMarble marble1, WarMarble marble2, int distance)
        {
            return Math.Abs(marble1.Position.X - marble2.Position.X) + Math.Abs(marble1.Position.Y - marble2.Position.Y) <= distance;
        }

        public void MoveMarble(WarMarble marble, int x, int y)
        {
            Tile oldTile = _tiles[marble.Position.X, marble.Position.Y];
            var newPosition = new TileCoordinate(marble.Position.X + x, marble.Position.Y + y);
            Tile newTile = _tiles[newPosition.X, newPosition.Y];

            oldTile.Element = null;
            newTile.Element = marble;
            marble.Position = newPosition;
        }
    }
}
