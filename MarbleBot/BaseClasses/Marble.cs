using Discord.Commands;
using System;

namespace MarbleBot.BaseClasses
{
    /// <summary> Marble in a Siege. </summary>
    public class Marble
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
        private int _HP;
        public int HP {
            get { return _HP; }
            set { _HP = value > MaxHP ? MaxHP : value < 1 ? 0 : value; }
        }
        public int MaxHP { get; private set; }
        public int DamageDealt { get; set; }
        public int PUHits { get; set; }
        public Item Shield { get; set; }
        public byte DamageIncrease { get; set; }
        public byte Evade { get; set; }
        public byte ItemAccuracy { get; set; } = 100;
        public bool BootsUsed { get; set; }
        public bool Cloned { get; set; }
        public bool QefpedunCharmUsed { get; set; }
        public MSE StatusEffect { get; set; }
        public DateTime DoomStart { get; set; }
        public DateTime LastPoisonTick { get; set; }
        public DateTime LastStun { get; set; }

        public Marble() {
            DoomStart = DateTime.Parse("2019-01-01 00:00:00");
            LastStun = DateTime.Parse("2019-01-01 00:00:00");
            LastPoisonTick = DateTime.Parse("2019-01-01 00:00:00");
        }

        public void DealDamage(int damage) {
            if (Shield.Id == 63) damage = (int)Math.Round(damage * 0.8);
            HP -= damage;
        }

        public void SetHP(int hp) {
            _HP = hp;
            MaxHP = hp;
        }

        public override string ToString() => $"{Name} HP: {HP}/20 [{Id}] BH: {DamageDealt} PH: {PUHits} Cloned: {Cloned}";
        
        public string ToString(SocketCommandContext context) {
            var user = context.Client.GetUser(Id);
            return $"**{Name}** (HP: **{HP}**/{MaxHP}, DMG: **{DamageDealt}**) [{user.Username}#{user.Discriminator}]";
        }
    }
}
