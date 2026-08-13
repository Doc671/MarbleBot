namespace MarbleBot;

public class BotCredentials
{
    public string Token { get; init; }
    public string? GoogleApiKey { get; init; }
    public ulong[] AdminUserIds { get; init; }
    public ulong[] DebugGuildIds { get; init; }

    public BotCredentials(string token, string? googleApiKey, ulong[] adminUserIds, ulong[] debugGuildIds)
    {
        Token = token;
        GoogleApiKey = googleApiKey;
        AdminUserIds = adminUserIds;
        DebugGuildIds = debugGuildIds;
    }
}
