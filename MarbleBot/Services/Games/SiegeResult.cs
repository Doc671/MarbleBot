namespace MarbleBot.Services.Games;

public class SiegeResult
{
    public string MarbleName { get; }
    public int FinalHealth { get; }
    public int DamageDealt { get; }
    public int Place { get; }

    public SiegeResult(string marbleName, int finalHealth, int damageDealt, int place)
    {
        MarbleName = marbleName;
        FinalHealth = finalHealth;
        DamageDealt = damageDealt;
        Place = place;
    }
}
