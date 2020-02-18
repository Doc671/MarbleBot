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

        public override bool Equals(object? obj)
        {
            return obj is Attack attack &&
                   Name == attack.Name &&
                   Damage == attack.Damage &&
                   Accuracy == attack.Accuracy &&
                   StatusEffect == attack.StatusEffect;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return 17 * Name.GetHashCode() + Damage.GetHashCode() + Accuracy.GetHashCode() + StatusEffect.GetHashCode();
            }
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
