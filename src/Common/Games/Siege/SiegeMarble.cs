using Discord.Commands;
using System;

namespace MarbleBot.Common.Games.Siege
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
        public DateTime LastMoveUsed { get; set; } = DateTime.MinValue;

        public SiegeMarble(ulong id, string name, int maxHealth) : base(id, name, maxHealth)
        {
        }

        public override string ToString()
        {
            return $"{Name} Health: {Health}/20 [{Id}] BH: {DamageDealt} PH: {PowerUpHits} Cloned: {Cloned}";
        }

        public string ToString(SocketCommandContext context, bool healthShown = true)
        {
            var marbleBotUser = MarbleBotUser.Find(Id);
            string usernameString;
            if (marbleBotUser != null)
            {
                usernameString = $"{marbleBotUser.Name}#{marbleBotUser.Discriminator}";
            }
            else
            {
                var discordSocketUser = context.Client.GetUser(Id);
                if (discordSocketUser != null)
                {
                    usernameString = $"{discordSocketUser.Username}#{discordSocketUser.Discriminator}";
                }
                else
                {
                    usernameString = "user not found";
                }
            }

            return healthShown
                ? $"**{Name}** (HP: **{Health}**/{MaxHealth}, DMG: **{DamageDealt}**) [{usernameString}]"
                : $"**{Name}** (DMG: **{DamageDealt}**) [{usernameString}]";
        }
    }
}
