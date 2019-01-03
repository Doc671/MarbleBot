using System;

namespace MarbleBot.Modules
{
    public class MoneyUser
    {
        public string Name { get; set; }
        public string Discriminator { get; set; }
        public ulong Money { get; set; }
        public uint DailyStreak { get; set; }
        public DateTime LastDaily { get; set; }
        public DateTime LastRaceWin { get; set;}
    }
}