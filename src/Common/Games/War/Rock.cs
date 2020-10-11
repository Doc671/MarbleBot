using Discord;

namespace MarbleBot.Common.Games.War
{
    public class Rock : ITileElement
    {
        private static readonly Emoji _rock = new Emoji("\uD83E\uDEA8");
        public Emoji DisplayEmoji => _rock;
        public TileCoordinate Position { get; set; }
    }
}
