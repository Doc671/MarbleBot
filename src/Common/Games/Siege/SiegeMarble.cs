using Discord.Commands;
using System;

namespace MarbleBot.Common
{
    public class SiegeMarble : BaseMarble
    {
        public int PowerUpHits { get; set; }
        public int Evade { get; set; }
        public bool BootsUsed { get; set; }
        public bool Cloned { get; set; }
        public bool QefpedunCharmUsed { get; set; }
        public StatusEffect StatusEffect { get; set; }
        public DateTime DoomStart { get; set; } = DateTime.MinValue;
        public DateTime LastPoisonTick { get; set; } = DateTime.MinValue;
        public DateTime LastStun { get; set; } = DateTime.MinValue;

        public SiegeMarble(ulong id, string name, int maxHealth) : base(id, name, maxHealth)
        {
        }

        public override string ToString() => $"{Name} Health: {Health}/20 [{Id}] BH: {DamageDealt} PH: {PowerUpHits} Cloned: {Cloned}";

        public string ToString(SocketCommandContext context, bool HealthShown = true)
        {
            var user = context.Client.GetUser(Id);
            if (HealthShown)
            {
                return $"**{Name}** (Health: **{Health}**/{MaxHealth}, DMG: **{DamageDealt}**) [{user.Username}#{user.Discriminator}]";
            }
            else
            {
                return $"**{Name}** (DMG: **{DamageDealt}**) [{user.Username}#{user.Discriminator}]";
            }
        }
    }
}
