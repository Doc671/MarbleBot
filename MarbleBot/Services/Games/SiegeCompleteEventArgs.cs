namespace MarbleBot.Services.Games;

public class SiegeCompleteEventArgs : EventArgs
{
    public ulong ChannelId { get; }
    public IReadOnlyList<SiegeResult> Results { get; }

    public SiegeCompleteEventArgs(ulong channelId, IReadOnlyList<SiegeResult> results)
    {
        ChannelId = channelId;
        Results = results;
    }
}
