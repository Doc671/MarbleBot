namespace MarbleBot.Entities;

public class User
{
    public ulong Id { get; init; }

    public string? Username { get; set; }
    public string? GlobalName { get; set; }

    public decimal Balance { get; set; }
    public decimal NetWorth { get; set; }
    public int Stage { get; set; } = 1;
    public int DailyStreak { get; set; }

    public bool SiegePing { get; set; }
    public bool WarPing { get; set; }

    public int RaceWins { get; set; }
    public int SiegeWins { get; set; }
    public int WarWins { get; set; }

    public string? WarEmoji { get; set; }

    public DateTime LastDaily { get; set; } = DateTime.MinValue;
    public DateTime LastRaceWin { get; set; } = DateTime.MinValue;
    public DateTime LastScavenge { get; set; } = DateTime.MinValue;
    public DateTime LastSiegeWin { get; set; } = DateTime.MinValue;
    public DateTime LastWarWin { get; set; } = DateTime.MinValue;

    public List<UserInventoryItem> Inventory { get; set; } = [];
}
