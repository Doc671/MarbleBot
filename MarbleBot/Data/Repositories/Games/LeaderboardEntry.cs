namespace MarbleBot.Data.Repositories.Games;

public class LeaderboardEntry
{
    public required string MarbleName { get; init; }
    public int Uses { get; init; }
    public int Wins { get; init; }
    public int Place { get; set; }
}
