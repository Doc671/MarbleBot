using MarbleBot.Services.Games;

namespace MarbleBot.Games.Siege;

public class AttackCompleteSiegeMessage : SiegeMessage
{
    public MemoryStream ImageStream { get; }

    public AttackCompleteSiegeMessage(string message, SiegeSessionState sessionState,  MemoryStream imageStream) : base(message, sessionState)
    {
        ImageStream = imageStream;
    }
}
