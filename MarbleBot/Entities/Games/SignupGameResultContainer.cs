using MarbleBot.Services.Games;

namespace MarbleBot.Entities.Games;

public class SignupGameResultContainer
{
    public int Id { get; set; }
    public required string MarbleName { get; init; }
    public required string UpperMarbleName { get; init; }
    public GameType GameType { get; init; }
    public ulong ChannelId { get; init; }
    public int Wins { get; set; }
    public int Uses { get; set; }
}
