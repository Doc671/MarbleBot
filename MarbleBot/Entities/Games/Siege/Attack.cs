using MarbleBot.Games.Siege;

namespace MarbleBot.Entities.Games.Siege;

public class Attack
{
    public int Id { get; init; }

    public ulong BossId { get; init; }
    public Boss Boss { get; init; } = null!;

    public string Name { get; init; } = null!;
    public int Damage { get; init; }
    public int Accuracy { get; init; }
    public StatusEffect StatusEffect { get; init; }
}
