using MarbleBot.Services.Games;

namespace MarbleBot.Entities.Games;

public class SignupGroup
{
    public int Id { get; init; }
    public ulong? ChannelId { get; init; }
    public GameType GameType { get; init; }

    public List<SignupEntry> Entries { get; init; } = [];
}
