using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

namespace MarbleBot.Services;

public class AutoModFilterService : IHostedService
{
    public record FilterCache(IReadOnlyCollection<string> BannedPatterns, DateTime LastCacheTime);

    private readonly DiscordSocketClient _client;
    private readonly ConcurrentDictionary<ulong, FilterCache> _guildFilterCache = new();

    public AutoModFilterService(DiscordSocketClient client)
    {
        _client = client;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _client.AutoModRuleCreated += rule => RemoveFromCache(rule.Guild.Id);
        _client.AutoModRuleUpdated += (_, rule) => RemoveFromCache(rule.Guild.Id);
        _client.AutoModRuleDeleted += rule => RemoveFromCache(rule.Guild.Id);
        return Task.CompletedTask;
    }

    private Task RemoveFromCache(ulong guildId)
    {
        _guildFilterCache.TryRemove(guildId, out _);
        return Task.CompletedTask;
    }

    public async Task<string> FilterAsync(string input, IInteractionContext context)
    {
        if (context.Guild == null)
        {
            return input;
        }

        var bannedPatterns = await GetBannedPatternsAsync(context.Guild);
        string[] inputWords = input.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string bannedPattern in bannedPatterns)
        {
            foreach (string inputWord in inputWords)
            {
                if (MatchesKeywordFilter(inputWord, bannedPattern))
                {
                    input = input.Replace(inputWord, new string('*', inputWord.Length), StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        return input;
    }

    public async Task<IReadOnlyCollection<string>> GetBannedPatternsAsync(IGuild guild)
    {
        if (_guildFilterCache.TryGetValue(guild.Id, out FilterCache? filterCache))
        {
            if ((DateTime.UtcNow - filterCache.LastCacheTime).Days < 1)
            {
                return filterCache.BannedPatterns;
            }

            _guildFilterCache.Remove(guild.Id, out _);
        }

        IAutoModRule[] autoModRules = await guild.GetAutoModRulesAsync();
        HashSet<string> bannedPatterns = autoModRules
            .Where(rule => rule.TriggerType == AutoModTriggerType.Keyword && rule.Enabled)
            .SelectMany(rule => rule.KeywordFilter)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        filterCache = new FilterCache(bannedPatterns, DateTime.UtcNow);
        _guildFilterCache[guild.Id] = filterCache;

        return bannedPatterns;
    }

    private static bool MatchesKeywordFilter(string text, string pattern)
    {
        bool prefix = pattern.StartsWith('*');
        bool suffix = pattern.EndsWith('*');
        string core = pattern.Trim('*');

        return (prefix, suffix) switch
        {
            (true, true) => text.Contains(core, StringComparison.OrdinalIgnoreCase),
            (false, true) => text.StartsWith(core, StringComparison.OrdinalIgnoreCase),
            (true, false) => text.EndsWith(core, StringComparison.OrdinalIgnoreCase),
            _ => text.Equals(core, StringComparison.OrdinalIgnoreCase)
        };
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
