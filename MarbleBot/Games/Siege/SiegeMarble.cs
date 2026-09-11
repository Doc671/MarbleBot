using MarbleBot.Entities;

namespace MarbleBot.Games.Siege;

public class SiegeMarble : Combatant
{
    public User User { get; set; } = null!;

    public int X { get; set; }
    public int Y { get; set; }

    public bool Dodging { get; set; }

    public DateTime LastAttack { get; set; }

    public SiegeMarble()
    {
    }

    public SiegeMarble(User user, string name, int health) : base(user.Id, name, health)
    {
        User = user;
    }
}
