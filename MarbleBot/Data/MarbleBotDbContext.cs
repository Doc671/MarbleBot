using MarbleBot.Data.Configurations;
using MarbleBot.Data.Configurations.Games;
using MarbleBot.Data.Converters;
using MarbleBot.Entities;
using MarbleBot.Entities.Games;
using Microsoft.EntityFrameworkCore;

namespace MarbleBot.Data;

public class MarbleBotDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<MarbleAppearance> MarbleAppearances => Set<MarbleAppearance>();
    public DbSet<MarbleAppearanceColor> MarbleAppearanceColors => Set<MarbleAppearanceColor>();

    public DbSet<Item> Items => Set<Item>();
    public DbSet<UserInventoryItem> UserInventoryItems => Set<UserInventoryItem>();
    public DbSet<CraftingRecipeIngredient> CraftingRecipeIngredients => Set<CraftingRecipeIngredient>();
    public DbSet<WeaponAmmo> WeaponAmmo => Set<WeaponAmmo>();

    public DbSet<SignupGroup> SignupGroups => Set<SignupGroup>();
    public DbSet<SignupEntry> SignupEntries => Set<SignupEntry>();
    public DbSet<SignupGameResultContainer> SignupGameResults => Set<SignupGameResultContainer>();

    public MarbleBotDbContext(DbContextOptions<MarbleBotDbContext> options) : base(options)
    {
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<ulong>().HaveConversion<UlongToLongConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        new UserEntityTypeConfiguration().Configure(modelBuilder.Entity<User>());

        new MarbleAppearanceEntityTypeConfiguration().Configure(modelBuilder.Entity<MarbleAppearance>());

        new MarbleAppearanceColorEntityTypeConfiguration().Configure(modelBuilder.Entity<MarbleAppearanceColor>());

        new UserInventoryItemEntityTypeConfiguration().Configure(modelBuilder.Entity<UserInventoryItem>());

        new ItemEntityTypeConfiguration().Configure(modelBuilder.Entity<Item>());

        new CraftingRecipeIngredientEntityTypeConfiguration().Configure(modelBuilder.Entity<CraftingRecipeIngredient>());

        new WeaponAmmoEntityTypeConfiguration().Configure(modelBuilder.Entity<WeaponAmmo>());

        new SignupGroupEntryEntityTypeConfiguration().Configure(modelBuilder.Entity<SignupEntry>());
    }
}
