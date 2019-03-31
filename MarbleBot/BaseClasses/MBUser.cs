using System;
using System.Collections.Generic;

namespace MarbleBot.BaseClasses
{
    /// <summary> Class for users when using money-based commands and games. </summary>
    public class MBUser 
    {
        public string Name { get; set; } = "";
        public string Discriminator { get; set; } = "0000";
        public decimal Balance { get; set; } = 0m;
        public decimal NetWorth { get; set; } = 0m;
        public uint DailyStreak { get; set; } = 0u;
        public bool SiegePing { get; set; } = false;
        public uint RaceWins { get; set; } = 0u;
        public uint SiegeWins { get; set; } = 0u;
        public DateTime LastDaily { get; set; } = DateTime.Parse("2019-01-01 00:00:00");
        public DateTime LastRaceWin { get; set; } = DateTime.Parse("2019-01-01 00:00:00");
        public DateTime LastSiegeWin { get; set; } = DateTime.Parse("2019-01-01 00:00:00");
        public Dictionary<int, int> Items { get; set; } = new Dictionary<int, int>();
    }
}