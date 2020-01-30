using System;

namespace MarbleBot.Common
{
    public abstract class BaseMarble
    {
        public ulong Id { get; set; }
        public string Name { get; set; } = "";
        private int _HP;
        public int HP
        {
            get => _HP;
            set => _HP = value > MaxHP ? MaxHP : value < 1 ? 0 : value;
        }
        public int MaxHP { get; private set; }
        public int DamageDealt { get; set; }
        public Item? Shield { get; set; }
        public int DamageIncrease { get; set; }
        public DateTime LastMoveUsed { get; set; } = DateTime.MinValue;


        public void DealDamage(int damage)
        {
            if (Shield != null && Shield.Id == 63)
            {
                damage = (int)Math.Round(damage * 0.8);
            }

            HP -= damage;
        }

        public void SetHP(int hp)
        {
            _HP = hp;
            MaxHP = hp;
        }
    }
}
