using System;
using System.Collections.Generic;

namespace MarbleBot.BaseClasses
{
    /// <summary> Class for users when using money-based commands and games. </summary>
    public class MBUser 
    {
        public string Name { get; set; } = "";
        public string Discriminator { get; set; } = "0000";
        public decimal Balance { get; set; }
        public decimal NetWorth { get; set; }
        public byte Stage { get; set; } = 1;
        public uint DailyStreak { get; set; }
        public bool SiegePing { get; set; }
        public uint RaceWins { get; set; }
        public uint SiegeWins { get; set; }
        public DateTime LastDaily { get; set; } = DateTime.Parse("2019-01-01 00:00:00");
        public DateTime LastRaceWin { get; set; } = DateTime.Parse("2019-01-01 00:00:00");
        public DateTime LastScavenge { get; set; } = DateTime.Parse("2019-01-01 00:00:00");
        public DateTime LastSiegeWin { get; set; } = DateTime.Parse("2019-01-01 00:00:00");
        public SortedDictionary<int, int> Items { get; set; } = new SortedDictionary<int, int>();

        public override string ToString() {
            return $"{Name}#{Discriminator}";
        }
    }
}