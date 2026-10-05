using MarbleBot.Services.Games;

namespace MarbleBot.Games.Siege;

public class BossChargingSiegeMessage : SiegeMessage
{
    public string AttackName { get; }
    public IReadOnlyList<SiegeMarble> TargetedMarbles { get; }
    public MemoryStream ImageStream { get; }

    public BossChargingSiegeMessage(string message, string attackName, SiegeSessionState sessionState, IReadOnlyList<SiegeMarble> targetedMarbles, MemoryStream imageStream) : base(message, sessionState)
    {
        AttackName = attackName;
        TargetedMarbles = targetedMarbles;
        ImageStream = imageStream;
    }
}
