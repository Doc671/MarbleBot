using MarbleBot.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace MarbleBot.Services;

public class GuildColorProvider
{
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly Dictionary<ulong, Color> _guildColorCache = [];

    public GuildColorProvider(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<Color> GetColorAsync(IInteractionContext context, CancellationToken cancellationToken = default)
    {
        if (context.Guild == null)
        {
            return Color.DarkerGrey;
        }

        if (_guildColorCache.TryGetValue(context.Guild.Id, out Color color))
        {
            return color;
        }

        using var scope = _scopeFactory.CreateScope();
        var guildDao = scope.ServiceProvider.GetRequiredService<GuildDao>();
        color = await guildDao.GetColorAsync(context.Guild.Id, cancellationToken);

        _guildColorCache[context.Guild.Id] = color;

        return color;
    }

    public void UpdateColor(ulong guildId, Color color)
    {
        if (_guildColorCache.ContainsKey(guildId))
        {
            _guildColorCache[guildId] = color;
        }
    }
}
