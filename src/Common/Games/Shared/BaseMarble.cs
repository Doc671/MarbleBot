using System;
using System.Linq;

namespace MarbleBot.Common
{
    public abstract class BaseMarble
    {
        public ulong Id { get; set; }
        public string Name { get; set; } = "";

        private int _health;
        public int Health
        {
            get => _health;
            set => _health = value > MaxHealth ? MaxHealth : value < 1 ? 0 : value;
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

        public int DamageDealt { get; set; }
        public Shield? Shield { get; set; }
        public Spikes? Spikes { get; set; }

        private int _damageBoost = 0;
        public int DamageBoost
        {
            get
            {
                return _damageBoost + (Spikes == null ? 0 : Spikes.DamageBoost);
            }
            set => _damageBoost = value;
        }

        public DateTime LastMoveUsed { get; set; } = DateTime.MinValue;

        protected BaseMarble(ulong id, string name, int maxHealth)
        {
            Id = id;
            Name = name;
            Health = maxHealth;
            MaxHealth = maxHealth;
        }

        public void DealDamage(int damage)
        {
            if (Shield != null && Shield.Id == 63)
            {
                damage = (int)Math.Round(damage * 0.8);
            }
            Health -= damage;
        }
    }
}
