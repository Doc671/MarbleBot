namespace MarbleBot.BaseClasses
{
    /// <summary> Represents a boss' attack during a Marble Siege. </summary>
    public readonly struct Attack {
        /// <summary> The name of the attack. </summary>
        public string Name { get; }
        /// <summary> The amount of damage dealt by the attack. </summary>
        public byte Damage { get; }
        /// <summary> The chance the attack will hit each marble out of 100. </summary>
        public byte Accuracy { get; }
        /// <summary> The status effect the attack inflicts. </summary>
        public MSE StatusEffect { get; }

        /// <summary> An empty instance of an attack. </summary>
        public static Attack Empty = new Attack("", 0, 0, MSE.None);

        /// <summary> Represents a boss' attack during a Marble Siege. </summary>
        /// <param name="name"> The name of the attack. </param>
        /// <param name="pwr"> The amount of damage dealt by the attack. </param>
        /// <param name="acc"> The chance the attack will hit each marble out of 100. </param>
        /// <param name="mse"> The status effect the attack inflicts. </param>
        public Attack(string name, byte pwr, byte acc, MSE mse) {
            Name = name;
            Damage = pwr;
            Accuracy = acc;
            StatusEffect = mse;
        }
    }
}