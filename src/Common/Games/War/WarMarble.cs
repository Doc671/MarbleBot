using System;

namespace MarbleBot.Common
{
    public class WarMarble : BaseMarble
    {
        public bool Boosted { get; set; } = false;
        public DateTime LastRage { get; set; } = DateTime.MinValue;
        public bool Rage { get; set; } = false;
        public int Team { get; set; }
        public WeaponClass WarClass { get; }
        public Weapon Weapon { get; }

        public WarMarble(ulong id, int HP, string name, Weapon weapon, Item shield, int spikeId = 0)
        {
            Id = id;
            SetHP(HP);
            WarClass = weapon.WarClass;
            DamageIncrease = spikeId switch
            {
                66 => 40,
                71 => 60,
                74 => 95,
                80 => 110,
                _ => 0
            };
            Name = name;
            Shield = shield;
            Weapon = weapon;
        }
    }
}
