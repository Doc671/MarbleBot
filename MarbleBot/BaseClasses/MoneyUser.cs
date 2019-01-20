using System;
using System.Collections.Generic;

namespace MarbleBot
{
    public class MoneyUser // Class for users when using money-based commands
    {
        public string Name { get; set; }
        public string Discriminator { get; set; }
        public decimal Balance { get; set; }
        public decimal NetWorth { get; set; }
        public uint DailyStreak { get; set; }
        public int RaceWins { get; set; }
        public DateTime LastDaily { get; set; }
        public DateTime LastRaceWin { get; set;}
        public Dictionary<int, int> Items { get; set; }
    }
}