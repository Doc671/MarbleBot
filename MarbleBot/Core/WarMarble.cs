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
                66 => (byte)40,
                71 => (byte)60,
                74 => (byte)95,
                80 => (byte)110,
                _ => (byte)0
            };
            Name = name;
            Shield = shield;
            Weapon = weapon;
        }
    }
}
