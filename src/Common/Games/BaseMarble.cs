using System;

namespace MarbleBot.Common.Games
{
    public abstract class BaseMarble
    {
        public float IncomingDamageMultiplier { get; set; } = 1f;
        public float OutgoingDamageMultiplier { get; set; } = 1f;

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

        private Shield? _shield;
        public Shield? Shield 
        { 
            get => _shield;
            set
            {
                float oldMultiplier = _shield?.IncomingDamageMultiplier ?? 1f;
                float newMultiplier = value?.IncomingDamageMultiplier ?? 1f;
                IncomingDamageMultiplier *= newMultiplier / oldMultiplier;
                _shield = value;
            }
        }

        private Spikes? _spikes;
        public Spikes? Spikes
        {
            get => _spikes;
            set
            {
                float oldMultiplier = _spikes?.OutgoingDamageMultiplier ?? 1f;
                float newMultiplier = value?.OutgoingDamageMultiplier ?? 1f;
                OutgoingDamageMultiplier *= newMultiplier / oldMultiplier;
                _spikes = value;
            }
        }

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
                damage = (int)MathF.Round(damage * IncomingDamageMultiplier);
            }

            Health -= damage;
        }
    }
}
