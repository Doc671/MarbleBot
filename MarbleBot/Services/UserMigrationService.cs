using MarbleBot.Data;
using MarbleBot.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MarbleBot.Services;

public class UserMigrationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ItemLoadedService _itemLoadedService;
    private readonly ILogger<UserMigrationService> _logger;

    public UserMigrationService(IServiceProvider serviceProvider, ItemLoadedService itemLoadedService, ILogger<UserMigrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _itemLoadedService = itemLoadedService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _itemLoadedService.Loaded.WaitAsync(cancellationToken);

        using var scope = _serviceProvider.CreateScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<MarbleBotDbContext>();

        await MigrateUsersAsync(databaseContext, cancellationToken);
    }

    private async Task MigrateUsersAsync(MarbleBotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Database already contains users. Skipping migration.");
            return;
        }

        if (!File.Exists("Users.json"))
        {
            _logger.LogInformation("Users.json file not found. Skipping migration.");
            return;
        }

        _logger.LogInformation("Starting legacy user migration...");

        using var stream = File.OpenRead("Users.json");
        var legacyUsers = await JsonSerializer.DeserializeAsync<Dictionary<ulong, LegacyUser>>(stream, cancellationToken: cancellationToken);
        if (legacyUsers == null)
        {
            _logger.LogWarning("No legacy users found in Users.json. Skipping migration.");
            return;
        }

        var newUsers = new List<User>();

        foreach ((ulong id, LegacyUser legacyUser) in legacyUsers)
        {
            var newUser = new User
            {
                Id = id,
                Balance = legacyUser.Balance,
                NetWorth = legacyUser.NetWorth,
                Stage = legacyUser.Stage,
                DailyStreak = legacyUser.DailyStreak,
                SiegePing = legacyUser.SiegePing,
                WarPing = legacyUser.WarPing,
                RaceWins = legacyUser.RaceWins,
                SiegeWins = legacyUser.SiegeWins,
                WarWins = legacyUser.WarWins,
                LastDaily = DateTime.SpecifyKind(legacyUser.LastDaily, DateTimeKind.Utc),
                LastRaceWin = DateTime.SpecifyKind(legacyUser.LastRaceWin, DateTimeKind.Utc),
                LastScavenge = DateTime.SpecifyKind(legacyUser.LastScavenge, DateTimeKind.Utc),
                LastSiegeWin = DateTime.SpecifyKind(legacyUser.LastSiegeWin, DateTimeKind.Utc),
                LastWarWin = DateTime.SpecifyKind(legacyUser.LastWarWin, DateTimeKind.Utc),
                Inventory = []
            };

            if (legacyUser.Items != null)
            {
                foreach ((int itemId, int quantity) in legacyUser.Items)
                {
                    if (quantity > 0)
                    {
                        var item = await dbContext.Items.FindAsync([itemId], cancellationToken: cancellationToken) ?? throw new InvalidOperationException($"Item {itemId} not found");
                        newUser.Inventory.Add(new UserInventoryItem
                        {
                            UserId = id,
                            ItemId = itemId,
                            Quantity = quantity
                        });
                    }
                }
            }

            newUsers.Add(newUser);
        }

        _logger.LogInformation("Migrating {newUsers.Count} users...", newUsers.Count);

        await dbContext.Users.AddRangeAsync(newUsers, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Legacy user migration completed successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
