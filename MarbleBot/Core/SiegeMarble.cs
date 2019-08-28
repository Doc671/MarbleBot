using Discord.Commands;
using System;

namespace MarbleBot.Core
{
    /// <summary> Represents a marble during a Siege game. </summary>
    public class SiegeMarble : BaseMarble
    {
        /// <summary> The number of times the marble has activated a power-up. </summary>
        public int PowerUpHits { get; set; }
        /// <summary> The marble's chance of dodging an attack. </summary>
        public int Evade { get; set; }
        /// <summary> Whether or not the Rocket Boots have been used. </summary>
        public bool BootsUsed { get; set; }
        /// <summary> Whether or not the marble is cloned. </summary>
        public bool Cloned { get; set; }
        /// <summary> Whether or not the Qefpedun Charm has been used. </summary>
        public bool QefpedunCharmUsed { get; set; }
        /// <summary> The marble's status ailment. </summary>
        public StatusEffect StatusEffect { get; set; }
        /// <summary> The time at which the marble had been doomed. </summary>
        public DateTime DoomStart { get; set; }
        /// <summary> The last time the marble was damaged by poison. </summary>
        public DateTime LastPoisonTick { get; set; }
        /// <summary> The last time the marble was stunned. </summary>
        public DateTime LastStun { get; set; }

        /// <summary> Represents a marble during a Siege game. </summary>
        public SiegeMarble()
        {
            DoomStart = DateTime.Parse("2019-01-01 00:00:00");
            LastStun = DateTime.Parse("2019-01-01 00:00:00");
            LastPoisonTick = DateTime.Parse("2019-01-01 00:00:00");
        }

        /// <summary> Converts this marble into a string representation. </summary>
        public override string ToString() => $"{Name} HP: {HP}/20 [{Id}] BH: {DamageDealt} PH: {PowerUpHits} Cloned: {Cloned}";

        /// <summary> Converts this marble into a string representation. </summary>
        /// <param name="context"> The command context. </param>
        public string ToString(SocketCommandContext context, bool HPShown = true)
        {
            var user = context.Client.GetUser(Id);
            if (HPShown)
                return $"**{Name}** (HP: **{HP}**/{MaxHP}, DMG: **{DamageDealt}**) [{user.Username}#{user.Discriminator}]";
            else
                return $"**{Name}** (DMG: **{DamageDealt}**) [{user.Username}#{user.Discriminator}]";
        }
    }
}
