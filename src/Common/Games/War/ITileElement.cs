using Discord;

namespace MarbleBot.Common.Games.War
{
    public interface ITileElement
    {
        Emoji DisplayEmoji { get; }
        TileCoordinate Position { get; set; }
    }
}
