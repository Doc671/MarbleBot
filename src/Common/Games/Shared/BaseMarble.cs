using System;

namespace MarbleBot.Common
{
    /// <summary> Represents the base marble class where Siege and War marbles are derived from. </summary>
    public abstract class BaseMarble
    {
        /// <summary> The Discord user ID of the marble's user. </summary>
        public ulong Id { get; set; }
        /// <summary> The name of the marble. </summary>
        public string Name { get; set; } = "";
        private int _HP;
        /// <summary> The number of health points the marble currently has. </summary>
        public int HP
        {
            get => _HP;
            set => _HP = value > MaxHP ? MaxHP : value < 1 ? 0 : value;
        }
        /// <summary> The maximum number of health points the marble can have. </summary>
        public int MaxHP { get; private set; }
        /// <summary> The amount of damage that has been dealt by the marble. </summary>
        public int DamageDealt { get; set; }
        /// <summary> The marble's shield. </summary>
        public Item Shield { get; set; }
        /// <summary> The marble's damage increase multiplier. </summary>
        public int DamageIncrease { get; set; }
        /// <summary> The last time the marble attacked. </summary>
        public DateTime LastMoveUsed { get; set; } = DateTime.MinValue;


        /// <summary> Deals damage to a marble. </summary>
        public void DealDamage(int damage)
        {
            if (Shield != null && Shield.Id == 63)
            {
                damage = (int)Math.Round(damage * 0.8);
            }

            HP -= damage;
        }

        /// <summary> Sets the HP of a marble. </summary>
        public void SetHP(int hp)
        {
            _HP = hp;
            MaxHP = hp;
        }
    }
}
