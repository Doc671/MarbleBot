using System.Linq;

namespace MarbleBot.Common
{
    public readonly struct Attack
    {
        public string Name { get; }
        public int Damage { get; }
        public int Accuracy { get; }
        public StatusEffect StatusEffect { get; }

        public static Attack Empty => new Attack("", 0, 0, StatusEffect.None);

        public Attack(string name, int damage, int accuracy, StatusEffect statusEffect)
        {
            Name = name;
            Damage = damage;
            Accuracy = accuracy;
            StatusEffect = statusEffect;
        }

        public override bool Equals(object? obj) => obj?.GetHashCode() == GetHashCode();

        public override int GetHashCode() => Name.Sum(c => c) + Damage + Accuracy + (int)StatusEffect;

        public static bool operator ==(Attack left, Attack right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Attack left, Attack right)
        {
            return !(left == right);
        }
    }
}
