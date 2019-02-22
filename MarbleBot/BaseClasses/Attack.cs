namespace MarbleBot.BaseClasses
{
    public class Attack {
        public string Name;
        public byte Damage;
        public byte Accuracy;
        public MSE StatusEffect;

        public static Attack Empty = new Attack("", 0, 0, MSE.None);
  
        public Attack(string name, byte pwr, byte acc, MSE mse) {
            Name = name;
            Damage = pwr;
            Accuracy = acc;
            StatusEffect = mse;
        }
    }
}