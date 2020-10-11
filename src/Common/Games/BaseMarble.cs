using System;

namespace MarbleBot.Common.Games
{
    public abstract class BaseMarble
    {
        public float DamageMultiplier { get; set; } = 1f;

        private int _health;

        public int Health
        {
            get => _health;
            set => _health = Math.Clamp(value, 0, MaxHealth);
        }

        private int _maxHealth;

        public int MaxHealth
        {
            get => _maxHealth;
            set
            {
                _health = value;
                _maxHealth = value;
            }
        }

        public ulong Id { get; protected set; }
        public string Name { get; protected set; }
        public int DamageDealt { get; set; }
        public Shield? Shield { get; set; }
        public Spikes? Spikes { get; set; }

        protected BaseMarble(ulong id, string name, int maxHealth)
        {
            Id = id;
            Name = name;
            Health = maxHealth;
            MaxHealth = maxHealth;
        }

        public void DealDamage(int damage)
        {
            if (Shield != null)
            {
                damage = (int)MathF.Round(damage * Shield.IncomingDamageMultiplier);
            }

            Health -= damage;
        }
    }
}
