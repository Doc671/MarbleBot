using System;

namespace MarbleBot.Common.Games.War
{
    public class WarMarble : BaseMarble
    {
        public bool Boosted { get; set; }
        public DateTime LastRage { get; set; } = DateTime.MinValue;
        public bool Rage { get; set; }
        public int Team { get; set; }
        public WeaponClass WeaponClass { get; }
        public Weapon Weapon { get; }

        public WarMarble(ulong id, string name, int maxHealth, Weapon weapon, Shield? shield, Spikes? spikes) : base(id, name, maxHealth)
        {
            Id = id;
            Health = MaxHealth = maxHealth;
            WeaponClass = weapon.WeaponClass;
            Name = name;
            Shield = shield;
            Spikes = spikes;
            Weapon = weapon;
        }
    }
}
