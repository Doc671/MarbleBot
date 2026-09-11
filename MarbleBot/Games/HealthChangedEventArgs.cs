namespace MarbleBot.Games;

public class HealthChangedEventArgs : EventArgs
{
    public string CombatantName { get; }
    public string? AttackName { get; }
    public int OldHealth { get; }
    public int NewHealth { get; }
    public int MaxHealth { get; }

    public HealthChangedEventArgs(string combatantName, string? attackName, int oldHealth, int newHealth, int maxHealth)
    {
        CombatantName = combatantName;
        AttackName = attackName;
        OldHealth = oldHealth;
        NewHealth = newHealth;
        MaxHealth = maxHealth;
    }
}
