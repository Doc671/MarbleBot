namespace MarbleBot.Core
{
    /// <summary> Represents a marble during a war game. </summary>
    public class WarMarble : BaseMarble
    {
        public int Team { get; set; }
        public WarClass WarClass { get; }
        public Item Weapon { get; }

        public WarMarble(ulong id, int HP, string name, Item weapon, Item shield, int spikeId = 0)
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
