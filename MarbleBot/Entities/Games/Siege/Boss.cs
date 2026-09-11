using MarbleBot.Games;
using MarbleBot.Games.Siege;

namespace MarbleBot.Entities.Games.Siege;

public class Boss : Combatant
{
    public List<Attack> Attacks { get; private set; } = [];
    public Difficulty Difficulty { get; private set; }
    public List<BossItemDrop> Drops { get; private set; } = [];
    public string ImageUrl { get; private set; } = null!;
    public int Stage { get; private set; }

    public void ApplyDefinition(LegacyBoss legacyBoss)
    {
        Name = legacyBoss.Name;
        MaxHealth = legacyBoss.Health;
        Health = legacyBoss.Health;
        Difficulty = legacyBoss.Difficulty;
        ImageUrl = legacyBoss.ImageUrl;
        Stage = legacyBoss.Stage;
    }
}
