namespace MarbleBot.Services.Games;

public class RaceResult
{
    public string MarbleName { get; }
    public string ResultMessage { get; }
    public int Place { get; }

    public RaceResult(string nickname, string resultMessage, int place)
    {
        MarbleName = nickname;
        ResultMessage = resultMessage;
        Place = place;
    }
}
