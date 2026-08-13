namespace MarbleBot.Entities;

public class LegacyUser
{
    public ulong Id { get; set; }
    public decimal Balance { get; set; }
    public decimal NetWorth { get; set; }
    public int Stage { get; set; }
    public int DailyStreak { get; set; }
    public bool SiegePing { get; set; }
    public bool WarPing { get; set; }
    public int RaceWins { get; set; }
    public int SiegeWins { get; set; }
    public int WarWins { get; set; }
    public DateTime LastDaily { get; set; }
    public DateTime LastRaceWin { get; set; }
    public DateTime LastScavenge { get; set; }
    public DateTime LastSiegeWin { get; set; }
    public DateTime LastWarWin { get; set; }

    public Dictionary<int, int>? Items { get; set; }
}
