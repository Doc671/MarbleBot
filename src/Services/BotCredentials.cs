﻿using System.Collections.Immutable;

namespace MarbleBot.Services
{
    public class BotCredentials
    {
        public string Token { get; set; }
        public string GoogleApiKey { get; set; }
        public ImmutableArray<ulong> AdminIds { get; set; }
        public ulong DebugChannel { get; set; }
    }
}
