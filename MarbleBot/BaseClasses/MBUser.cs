using System;
using System.Collections.Generic;

namespace MarbleBot.BaseClasses
{
    public class MBUser // Class for users when using money-based commands and games
    {
        public string Name { get; set; }
        public string Discriminator { get; set; }
        public decimal Balance { get; set; }
        public decimal NetWorth { get; set; }
        public uint DailyStreak { get; set; }
        public uint RaceWins { get; set; }
        public uint SiegeWins { get; set; }
        public DateTime LastDaily { get; set; }
        public DateTime LastRaceWin { get; set; }
        public DateTime LastSiegeWin { get; set; }
        public Dictionary<int, int> Items { get; set; }
    }
}