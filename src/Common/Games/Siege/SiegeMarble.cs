using Discord.Commands;
using MarbleBot.Modules;
using System;
using System.Threading.Tasks;

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

        public async Task<string> ToString(SocketCommandContext context, bool healthShown = true)
        {
            string usernameString = await MarbleBotModule.GetUsernameDiscriminatorString(context, Id);

            return healthShown
                ? $"**{Name}** (HP: **{Health}**/{MaxHealth}, DMG: **{DamageDealt}**) [{usernameString}]"
                : $"**{Name}** (DMG: **{DamageDealt}**) [{usernameString}]";
        }
    }
}
