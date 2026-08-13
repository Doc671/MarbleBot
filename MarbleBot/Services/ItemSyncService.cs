using MarbleBot.Data;
using MarbleBot.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MarbleBot.Services;

public class ItemSyncService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ItemLoadedService _itemLoadedService;

    private readonly ILogger<UserMigrationService> _logger;

    public ItemSyncService(IServiceProvider serviceProvider, ItemLoadedService itemLoadedService, ILogger<UserMigrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _itemLoadedService = itemLoadedService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<MarbleBotDbContext>();

        await MigrateItemsAsync(databaseContext, cancellationToken);
    }

    private async Task MigrateItemsAsync(MarbleBotDbContext dbContext, CancellationToken cancellationToken)
    {
        string itemsFilePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Items.json");
        if (!File.Exists(itemsFilePath))
        {
            _logger.LogInformation("Items.json file not found. Skipping migration.");
            return;
        }

        _logger.LogInformation("Starting legacy item data migration...");

        using var stream = File.OpenRead(itemsFilePath);
        var legacyItems = await JsonSerializer.DeserializeAsync<Dictionary<int, LegacyItem>>(stream, cancellationToken: cancellationToken);
        if (legacyItems == null)
        {
            _logger.LogWarning("No legacy items found in Items.json. Skipping migration.");
            return;
        }

        var newItems = new List<Item>();

        foreach ((int id, LegacyItem legacyItem) in legacyItems)
        {
            if (await dbContext.Items.AnyAsync(i => i.Id == id, cancellationToken))
            {
                var existingItem = await dbContext.Items.FirstAsync(i => i.Id == id, cancellationToken);
                Item newItem = CreateItemFromLegacy(id, legacyItem);
                if (existingItem == null)
                {
                    newItems.Add(newItem);
                }
                else
                {
                    existingItem.Name = legacyItem.Name;
                    existingItem.Description = legacyItem.Description;
                    existingItem.Value = legacyItem.Price;
                    existingItem.Stage = legacyItem.Stage;
                    existingItem.ScavengeLocation = legacyItem.ScavengeLocation;
                    existingItem.CraftingProduced = legacyItem.CraftingProduced;
                    existingItem.CraftingStationRequired = legacyItem.CraftingStationRequired;

                    if (existingItem.GetType() == newItem.GetType())
                    {
                        switch (existingItem)
                        {
                            case Weapon existingWeapon when newItem is Weapon newWeapon:
                                existingWeapon.Accuracy = newWeapon.Accuracy;
                                existingWeapon.Damage = newWeapon.Damage;
                                existingWeapon.Hits = newWeapon.Hits;
                                existingWeapon.WeaponClass = newWeapon.WeaponClass;
                                break;
                            case Shield existingShield when newItem is Shield newShield:
                                existingShield.IncomingDamageMultiplier = newShield.IncomingDamageMultiplier;
                                break;
                            case Spikes existingSpikes when newItem is Spikes newSpikes:
                                existingSpikes.OutgoingDamageMultiplier = newSpikes.OutgoingDamageMultiplier;
                                break;
                            case Ammo existingAmmo when newItem is Ammo newAmmo:
                                existingAmmo.Damage = newAmmo.Damage;
                                break;
                        }

                        dbContext.Items.Update(existingItem);
                    }
                    else
                    {
                        dbContext.Items.Remove(existingItem);
                        dbContext.Items.Add(newItem);
                    }
                }
            }
            else
            {
                Item item = CreateItemFromLegacy(id, legacyItem);

                newItems.Add(item);
            }
        }

        _logger.LogInformation("Migrating {newItems.Count} items...", newItems.Count);

        await dbContext.Items.AddRangeAsync(newItems, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Legacy item data migration completed successfully.");

        _itemLoadedService.SetLoaded();
    }

    private static Item CreateItemFromLegacy(int id, LegacyItem legacyItem)
    {
        Item item;
        if (legacyItem.WeaponClass != null)
        {
            var weapon = new Weapon
            {
                Id = id,
                Name = legacyItem.Name,
                Description = legacyItem.Description,
                Value = legacyItem.Price,
                Stage = legacyItem.Stage,
                ScavengeLocation = legacyItem.ScavengeLocation,
                CraftingProduced = legacyItem.CraftingProduced,
                CraftingStationRequired = legacyItem.CraftingStationRequired,
                Accuracy = legacyItem.Accuracy.GetValueOrDefault(),
                Damage = legacyItem.Damage.GetValueOrDefault(),
                Hits = legacyItem.Hits.GetValueOrDefault(),
                WeaponClass = legacyItem.WeaponClass.Value,
            };
            item = weapon;
        }
        else if (legacyItem.IncomingDamageMultiplier != null)
        {
            var shield = new Shield
            {
                Id = id,
                Name = legacyItem.Name,
                Description = legacyItem.Description,
                Value = legacyItem.Price,
                Stage = legacyItem.Stage,
                ScavengeLocation = legacyItem.ScavengeLocation,
                CraftingProduced = legacyItem.CraftingProduced,
                CraftingStationRequired = legacyItem.CraftingStationRequired,
                IncomingDamageMultiplier = legacyItem.IncomingDamageMultiplier.GetValueOrDefault(),
            };
            item = shield;
        }
        else if (legacyItem.OutgoingDamageMultiplier != null)
        {
            var spikes = new Spikes
            {
                Id = id,
                Name = legacyItem.Name,
                Description = legacyItem.Description,
                Value = legacyItem.Price,
                Stage = legacyItem.Stage,
                ScavengeLocation = legacyItem.ScavengeLocation,
                CraftingProduced = legacyItem.CraftingProduced,
                CraftingStationRequired = legacyItem.CraftingStationRequired,
                OutgoingDamageMultiplier = legacyItem.OutgoingDamageMultiplier.GetValueOrDefault(),
            };
            item = spikes;
        }
        else
        {
            item = new Item
            {
                Id = id,
                Name = legacyItem.Name,
                Description = legacyItem.Description,
                Value = legacyItem.Price,
                Stage = legacyItem.Stage,
                ScavengeLocation = legacyItem.ScavengeLocation,
                CraftingProduced = legacyItem.CraftingProduced,
                CraftingStationRequired = legacyItem.CraftingStationRequired
            };
        }

        return item;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
