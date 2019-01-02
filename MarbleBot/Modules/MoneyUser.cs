using System;
using System.Collections.Generic;

namespace MarbleBot.Modules
{
    public class MoneyUser
    {
        public string Name { get; set; }
        public ushort Discriminator { get; set; }
        public ulong Money { get; set; }
        public uint DailyStreak { get; set; }
        public DateTime LastDaily { get; set; }
    }
}