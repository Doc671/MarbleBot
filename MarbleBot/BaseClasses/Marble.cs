using Discord.Commands;
using System;

namespace MarbleBot.BaseClasses
{
    /// <summary> Represents a marble during a Siege game. </summary>
    public class Marble
    {
        /// <summary> The Discord user ID of the marble's user. </summary>
        public ulong Id { get; set; }
        /// <summary> The name of the marble. </summary>
        public string Name { get; set; }
        private int _HP;
        /// <summary> The number of health points the marble currently has. </summary>
        public int HP
        {
            get => _HP;
            set { _HP = value > MaxHP ? MaxHP : value < 1 ? 0 : value; }
        }
        /// <summary> The maximum number of health points the marble can have. </summary>
        public int MaxHP { get; private set; }
        /// <summary> The amount of damage that has been dealt by the marble. </summary>
        public int DamageDealt { get; set; }
        /// <summary> The number of times the marble has activated a power-up. </summary>
        public int PowerUpHits { get; set; }
        /// <summary> The marble's shield. </summary>
        public Item Shield { get; set; }
        /// <summary> The marble's damage increase multiplier. </summary>
        public byte DamageIncrease { get; set; }
        /// <summary> The marble's chance of dodging an attack. </summary>
        public byte Evade { get; set; }
        /// <summary> The marble's chance of an offensive item dealing damage. </summary>
        public byte ItemAccuracy { get; set; } = 100;
        /// <summary> Whether or not the Rocket Boots have been used. </summary>
        public bool BootsUsed { get; set; }
        /// <summary> Whether or not the marble is cloned. </summary>
        public bool Cloned { get; set; }
        /// <summary> Whether or not the Qefpedun Charm has been used. </summary>
        public bool QefpedunCharmUsed { get; set; }
        /// <summary> The marble's status ailment. </summary>
        public MSE StatusEffect { get; set; }
        /// <summary> The time at which the marble had been doomed. </summary>
        public DateTime DoomStart { get; set; }
        /// <summary> The last time the marble was damaged by poison. </summary>
        public DateTime LastPoisonTick { get; set; }
        /// <summary> The last time the marble was stunned. </summary>
        public DateTime LastStun { get; set; }

        /// <summary> Represents a marble during a Siege game. </summary>
        public Marble()
        {
            DoomStart = DateTime.Parse("2019-01-01 00:00:00");
            LastStun = DateTime.Parse("2019-01-01 00:00:00");
            LastPoisonTick = DateTime.Parse("2019-01-01 00:00:00");
        }

        /// <summary> Deals damage to a marble. </summary>
        public void DealDamage(int damage)
        {
            if (Shield.Id == 63) damage = (int)Math.Round(damage * 0.8);
            HP -= damage;
        }

        /// <summary> Sets the HP of a marble. </summary>
        public void SetHP(int hp)
        {
            _HP = hp;
            MaxHP = hp;
        }

        /// <summary> Converts this marble into a string representation. </summary>
        public override string ToString() => $"{Name} HP: {HP}/20 [{Id}] BH: {DamageDealt} PH: {PowerUpHits} Cloned: {Cloned}";

        /// <summary> Converts this marble into a string representation. </summary>
        /// <param name="context"> The command context. </param>
        public string ToString(SocketCommandContext context)
        {
            var user = context.Client.GetUser(Id);
            return $"**{Name}** (HP: **{HP}**/{MaxHP}, DMG: **{DamageDealt}**) [{user.Username}#{user.Discriminator}]";
        }
    }
}
