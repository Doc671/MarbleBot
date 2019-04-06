using System;

namespace MarbleBot.BaseClasses
{
    public class Marble
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
        public int HP { get; set; }
        public int MaxHP { get; set; }
        public int BossHits { get; set; }
        public int PUHits { get; set; }
        public bool Cloned { get; set; }
        public bool QefpedunCharmUsed { get; set; }
        public MSE StatusEffect { get; set; }
        public DateTime DoomStart { get; set; }
        public DateTime LastPoisonTick { get; set; }
        public DateTime LastStun { get; set; }

        public Marble()
        {
            BossHits = 0;
            PUHits = 0;
            Cloned = false;
            QefpedunCharmUsed = false;
            StatusEffect = MSE.None;
            DoomStart = DateTime.Parse("2019-01-01 00:00:00");
            LastStun = DateTime.Parse("2019-01-01 00:00:00");
            LastPoisonTick = DateTime.Parse("2019-01-01 00:00:00");
        }

        public override string ToString()
        {
            return $"{Name} HP: {HP}/20 [{Id}] BH: {BossHits} PH: {PUHits} Cloned: {Cloned}";
        }
    }
}
