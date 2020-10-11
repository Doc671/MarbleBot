using Discord;

namespace MarbleBot.Common.Games.War
{
    public class Tile
    {
        private static readonly Emoji _largeGreenSquare = new Emoji("\uD83D\uDFE9");
        public Emoji DisplayEmoji => Element == null ? _largeGreenSquare : Element.DisplayEmoji;
        public ITileElement? Element { get; set; }
    }
}
