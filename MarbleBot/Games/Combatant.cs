namespace MarbleBot.Games;

public class Combatant
{
    public ulong Id { get; private set; }

    public string Name { get; protected set; } = null!;

    private int _health;

    public int Health
    {
        get => _health;
        set
        {
            int newHealth = Math.Clamp(value, 0, MaxHealth);
            if (_health == newHealth)
            {
                return;
            }

            int oldHealth = _health;
            _health = newHealth;

            if (HealthChanged != null && _attackerName != null)
            {
                var eventArgs = GetHealthArgs(oldHealth, newHealth);
                HealthChanged.Invoke(this, eventArgs);
            }
        }
    }

    public int MaxHealth { get; protected set; }

    private string? _attackerName;

    public event EventHandler<HealthChangedEventArgs>? HealthChanged;

    public Combatant()
    {
    }

    public Combatant(ulong id, string name, int health)
    {
        Id = id;
        Name = name;
        _health = MaxHealth = health;
    }

    public void Damage(int damage, string attackName)
    {
        _attackerName = attackName;

        if (damage == 0)
        {
            return;
        }

        Health -= damage;
    }

    protected virtual HealthChangedEventArgs GetHealthArgs(int oldHealth, int newHealth)
    {
        return new HealthChangedEventArgs(Name, _attackerName, oldHealth, newHealth, MaxHealth);
    }
}
