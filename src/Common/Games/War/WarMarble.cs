using Discord;
using System;

namespace MarbleBot.Common.Games.War
{
    public class WarMarble : BaseMarble, ITileElement
    {
        public bool Boosted { get; set; }
        public Emoji DisplayEmoji { get; }
        public DateTime LastRage { get; set; } = DateTime.MinValue;
        public TileCoordinate Position { get; set; }
        public bool Rage { get; set; }
        public WarTeam? Team { get; set; }
        public Weapon Weapon { get; }
        public string Username { get; }
        public string Discriminator { get; }

        public WarMarble(ulong id, string name, int maxHealth, Weapon weapon, Shield? shield, Spikes? spikes, string username, string discriminator) : base(id, name, maxHealth)
        {
            Id = id;
            Health = MaxHealth = maxHealth;
            Name = name;
            Shield = shield;
            Spikes = spikes;
            Weapon = weapon;
            Username = username;
            Discriminator = discriminator;

            const string robotFace = "\uD83E\uDD16";
            DisplayEmoji = new Emoji(robotFace);
        }

        public WarMarble(MarbleBotUser user, string name, int maxHealth, Weapon weapon) : base(user.Id, name, maxHealth)
        {
            DisplayEmoji = new Emoji(user.WarEmoji);
            Shield = user.GetShield();
            Spikes = user.GetSpikes();
            Weapon = weapon;
            Username = user.Name;
            Discriminator = user.Discriminator;
        }
    }
}
