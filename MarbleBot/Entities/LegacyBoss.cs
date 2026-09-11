using MarbleBot.Entities.Games.Siege;
using MarbleBot.Games.Siege;

namespace MarbleBot.Entities;

public class LegacyBoss
{
    public int Health { get; init; }
    public string Name { get; init; } = null!;
    public List<Attack> Attacks { get; init; } = [];
    public Difficulty Difficulty { get; init; }
    public List<BossItemDrop> Drops { get; init; } = [];
    public string ImageUrl { get; init; } = null!;
    public int Stage { get; init; }
}
