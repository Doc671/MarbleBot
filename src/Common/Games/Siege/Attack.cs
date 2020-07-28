using System;

namespace MarbleBot.Common.Games.Siege
{
    public readonly struct Attack : IEquatable<Attack>
    {
        public string Name { get; }
        public int Damage { get; }
        public int Accuracy { get; }
        public StatusEffect StatusEffect { get; }

        public Attack(string name, int damage, int accuracy, StatusEffect statusEffect)
        {
            Name = name;
            Damage = damage;
            Accuracy = accuracy;
            StatusEffect = statusEffect;
        }

        public override bool Equals(object? obj)
        {
            return obj is Attack attack &&
                   Name == attack.Name &&
                   Damage == attack.Damage &&
                   Accuracy == attack.Accuracy &&
                   StatusEffect == attack.StatusEffect;
        }

        public bool Equals(Attack other)
        {
            return Name == other.Name &&
                   Damage == other.Damage &&
                   Accuracy == other.Accuracy &&
                   StatusEffect == other.StatusEffect;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Damage, Accuracy, StatusEffect);
        }

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
