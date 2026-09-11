using MarbleBot.Data;
using MarbleBot.Entities;
using MarbleBot.Entities.Games.Siege;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MarbleBot.Services.Games;

public class BossSyncService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BossLoadedService _bossLoadedService;
    private readonly ILogger<BossSyncService> _logger;

    public BossSyncService(IServiceProvider serviceProvider, BossLoadedService bossLoadedService, ILogger<BossSyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _bossLoadedService = bossLoadedService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MarbleBotDbContext>();

        await MigrateBossesAsync(dbContext, cancellationToken);
    }

    private async Task MigrateBossesAsync(MarbleBotDbContext dbContext, CancellationToken cancellationToken)
    {
        string itemsFilePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Bosses.json");
        if (!File.Exists(itemsFilePath))
        {
            _logger.LogInformation("Bosses.json file not found. Skipping migration.");
            return;
        }

        _logger.LogInformation("Starting legacy boss data migration...");

        using var stream = File.OpenRead(itemsFilePath);
        var bosses = await JsonSerializer.DeserializeAsync<Dictionary<string, LegacyBoss>>(stream, cancellationToken: cancellationToken);
        if (bosses == null)
        {
            _logger.LogWarning("No legacy bosses found in Bosses.json. Skipping migration.");
            return;
        }

        var newBosses = new List<Boss>();
        var bossesToRemove = new List<Boss>();
        foreach ((string name, LegacyBoss definitionBoss) in bosses)
        {
            Boss? existingBoss = await dbContext.Bosses
                .Include(boss => boss.Attacks)
                .Include(boss => boss.Drops)
                .SingleOrDefaultAsync(boss => boss.Name == definitionBoss.Name, cancellationToken);

            if (existingBoss == null)
            {
                var newBoss = new Boss();
                newBoss.ApplyDefinition(definitionBoss);

                foreach (var definitionAttack in definitionBoss.Attacks)
                {
                    newBoss.Attacks.Add(new Attack
                    {
                        Name = definitionAttack.Name,
                        Damage = definitionAttack.Damage,
                        Accuracy = definitionAttack.Accuracy,
                        StatusEffect = definitionAttack.StatusEffect
                    });
                }

                foreach (var definitionDrop in definitionBoss.Drops)
                {
                    newBoss.Drops.Add(new BossItemDrop
                    {
                        ItemId = definitionDrop.ItemId,
                        MinCount = definitionDrop.MinCount,
                        MaxCount = definitionDrop.MaxCount,
                        Chance = definitionDrop.Chance
                    });
                }

                newBosses.Add(newBoss);
            }
            else
            {
                existingBoss.ApplyDefinition(definitionBoss);

                CompareAttacks(dbContext, definitionBoss, existingBoss);
                CompareBossDrops(dbContext, definitionBoss, existingBoss);
            }
        }

        await dbContext.Bosses.AddRangeAsync(newBosses, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        _bossLoadedService.SetLoaded();
    }

    private static void CompareAttacks(MarbleBotDbContext dbContext, LegacyBoss definitionBoss, Boss existingBoss)
    {
        HashSet<int> definitionAttackIds = definitionBoss.Attacks
            .Select(attack => attack.Id)
            .ToHashSet();

        foreach (Attack existingAttack in existingBoss.Attacks
            .Where(attack => !definitionAttackIds.Contains(attack.Id))
            .ToList())
        {
            dbContext.Attacks.Remove(existingAttack);
        }

        foreach (Attack definitionAttack in definitionBoss.Attacks)
        {
            if (!existingBoss.Attacks.Any(attack => attack.Id == definitionAttack.Id))
            {
                existingBoss.Attacks.Add(new Attack
                {
                    BossId = existingBoss.Id,
                    Name = definitionAttack.Name,
                    Damage = definitionAttack.Damage,
                    Accuracy = definitionAttack.Accuracy,
                    StatusEffect = definitionAttack.StatusEffect
                });
            }
        }
    }

    private static void CompareBossDrops(MarbleBotDbContext dbContext, LegacyBoss definitionBoss, Boss existingBoss)
    {
        HashSet<int> definitionDropItemIds = definitionBoss.Drops
            .Select(drops => drops.ItemId)
            .ToHashSet();

        foreach (BossItemDrop existingDrop in existingBoss.Drops
            .Where(drop => !definitionDropItemIds.Contains(drop.ItemId))
            .ToList())
        {
            dbContext.BossItemDrops.Remove(existingDrop);
        }

        foreach (BossItemDrop definitionDrop in definitionBoss.Drops)
        {
            if (!existingBoss.Drops.Any(drop => drop.ItemId == definitionDrop.ItemId))
            {
                existingBoss.Drops.Add(new BossItemDrop
                {
                    BossId = existingBoss.Id,
                    ItemId = definitionDrop.ItemId,
                    MinCount = definitionDrop.MinCount,
                    MaxCount = definitionDrop.MaxCount,
                    Chance = definitionDrop.Chance
                });
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
