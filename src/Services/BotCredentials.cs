using Google.Apis.Auth.OAuth2;
using System.Collections.Immutable;

namespace MarbleBot.Services
{
    public class BotCredentials
    {
        public string Token { get; set; }
        public string GoogleApiKey { get; set; }
        public ImmutableArray<ulong> AdminIds { get; set; }
        public ulong DebugChannel { get; set; }
        public UserCredential GoogleUserCredential { get; set; }

        public BotCredentials(string token, string googleApiKey, ImmutableArray<ulong> adminIds, ulong debugChannel,
            UserCredential googleUserCredential)
        {
            Token = token;
            GoogleApiKey = googleApiKey;
            AdminIds = adminIds;
            DebugChannel = debugChannel;
            GoogleUserCredential = googleUserCredential;
        }
    }
}
