using MarbleBot.Services.Games;

namespace MarbleBot.Entities.Games;

public class SignupEntry
{
    public int Id { get; init; }

    public int SignupGroupId { get; init; }
    public SignupGroup SignupGroup { get; set; } = null!;

    public ulong UserId { get; init; }
    public User User { get; set; } = null!;

    public GameType GameType { get; init; }

    public string MarbleName { get; set; } = null!;
}
