using MarbleBot.Data;
using MarbleBot.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MarbleBot.Services;

public class GuildMigrationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GuildMigrationService> _logger;

    public GuildMigrationService(IServiceProvider serviceProvider, ILogger<GuildMigrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MarbleBotDbContext>();
        await MigrateGuildsAsync(dbContext, cancellationToken);
    }

    private async Task MigrateGuildsAsync(MarbleBotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Guilds.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Database already contains guilds. Skipping migration.");
            return;
        }

        if (!File.Exists("Guilds.json"))
        {
            _logger.LogInformation("Guilds.json file not found. Skipping migration.");
            return;
        }

        _logger.LogInformation("Starting legacy guild migration...");

        using var stream = File.OpenRead("Guilds.json");
        var legacyGuilds = await JsonSerializer.DeserializeAsync<Dictionary<ulong, LegacyGuild>>(stream, cancellationToken: cancellationToken);
        if (legacyGuilds == null)
        {
            _logger.LogWarning("No legacy guilds found in Guilds.json. Skipping migration.");
            return;
        }

        var newGuilds = new List<Guild>();
        var newGuildUsableChannels = new List<GuildUsableChannel>();
        foreach ((ulong id, LegacyGuild legacyGuild) in legacyGuilds)
        {
            var newGuild = new Guild()
            {
                Id = id,
                Color = ColorExtensions.FromHexCode(legacyGuild.Color)
            };

            if (legacyGuild.UsableChannels != null)
            {
                foreach (ulong channelId in legacyGuild.UsableChannels)
                {
                    var guildUsableChannel = new GuildUsableChannel()
                    {
                        GuildId = channelId,
                        ChannelId = channelId
                    };

                    newGuild.UsableChannels.Add(guildUsableChannel);
                    newGuildUsableChannels.Add(guildUsableChannel);
                }
            }

            newGuilds.Add(newGuild);
        }

        _logger.LogInformation("Migrating {newGuilds.Count} users...", newGuilds.Count);

        await dbContext.Guilds.AddRangeAsync(newGuilds, cancellationToken);
        await dbContext.GuildUsableChannels.AddRangeAsync(newGuildUsableChannels, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Legacy guild migration completed successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
