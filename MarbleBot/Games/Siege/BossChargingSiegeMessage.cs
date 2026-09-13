using MarbleBot.Services.Games;

namespace MarbleBot.Games.Siege;

public class BossChargingSiegeMessage : SiegeMessage
{
    public IReadOnlyList<ulong> TargetedIds { get; }
    public MemoryStream ImageStream { get; }

    public BossChargingSiegeMessage(string message, SiegeSessionState sessionState, IReadOnlyList<ulong> targetedIds, MemoryStream imageStream) : base(message, sessionState)
    {
        TargetedIds = targetedIds;
        ImageStream = imageStream;
    }
}
