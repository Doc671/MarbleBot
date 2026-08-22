namespace MarbleBot.Entities;

public class Guild
{
    public ulong Id { get; init; }
    public uint Color { get; set; }
    public List<GuildUsableChannel> UsableChannels { get; set; } = [];
}
