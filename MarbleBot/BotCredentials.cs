namespace MarbleBot;

public class BotCredentials
{
    public string Token { get; init; }
    public string? GoogleApiKey { get; init; }
    public string? GoogleServiceAccountKeyPath { get; init; }
    public ulong[] AdminUserIds { get; init; }
    public ulong[] DebugGuildIds { get; init; }

    public BotCredentials(string token, string? googleApiKey, string? googleServiceAccountKeyPath, ulong[] adminUserIds, ulong[] debugGuildIds)
    {
        Token = token;
        GoogleApiKey = googleApiKey;
        GoogleServiceAccountKeyPath = googleServiceAccountKeyPath;
        AdminUserIds = adminUserIds;
        DebugGuildIds = debugGuildIds;
    }
}
