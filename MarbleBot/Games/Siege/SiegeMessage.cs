using MarbleBot.Services.Games;

namespace MarbleBot.Games.Siege;

public class SiegeMessage
{
    public string Message { get; }
    public SiegeSessionState SessionState { get; }

    public SiegeMessage(string message, SiegeSessionState sessionState)
    {
        Message = message;
        SessionState = sessionState;
    }
}
