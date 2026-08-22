namespace MarbleBot.Entities;

public class GuildUsableChannel
{
    public ulong GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public ulong ChannelId { get; set; }
}
