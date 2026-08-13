global using Discord;
global using Discord.Interactions;
global using Discord.WebSocket;
global using MarbleBot.Extensions;
global using NLog;
using MarbleBot.Data;
using MarbleBot.Data.Repositories;
using MarbleBot.Data.Repositories.Games;
using MarbleBot.Services;
using MarbleBot.Services.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace MarbleBot;

internal sealed class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        string botCredentialsPath = Path.Combine(AppContext.BaseDirectory, "Resources", "BotCredentials.json");
        builder.Configuration.AddJsonFile(botCredentialsPath, optional: false, reloadOnChange: true);

        var botCredentials = builder.Configuration.Get<BotCredentials>() ?? throw new InvalidDataException("Cannot bind BotCredentials.json");
        builder.Services.AddSingleton(botCredentials);

        string databaseSettingsPath = Path.Combine(AppContext.BaseDirectory, "Resources", "DatabaseSettings.json");
        builder.Configuration.AddJsonFile(databaseSettingsPath, optional: false, reloadOnChange: true);

        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
        builder.Logging.AddNLog();

        AddDatabaseServices(builder);
        AddBotServices(builder);

        var host = builder.Build();
        await host.RunAsync();
    }

    private static void AddDatabaseServices(HostApplicationBuilder builder)
    {
        builder.Services.AddDbContext<MarbleBotDbContext>(options =>
        {
            string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            options.UseNpgsql(connectionString);
        });

        builder.Services.AddScoped<UserDao>();
        builder.Services.AddScoped<ItemDao>();
        builder.Services.AddScoped<SignupGroupDao>();
        builder.Services.AddScoped<SignupGameResultDao>();
    }

    private static void AddBotServices(HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.DirectMessages,
            AlwaysDownloadUsers = true
        });

        builder.Services.AddSingleton(provider => new DiscordSocketClient(provider.GetRequiredService<DiscordSocketConfig>()))
            .AddSingleton(provider => new InteractionService(provider.GetRequiredService<DiscordSocketClient>(), new InteractionServiceConfig
            {
#if DEBUG
                LogLevel = LogSeverity.Debug,
#else
                LogLevel = LogSeverity.Info,
#endif
                DefaultRunMode = RunMode.Async,
                UseCompiledLambda = true
            }));

        builder.Services.AddSingleton<ItemLoadedService>();

        builder.Services.AddSingleton<SlashCommandHandler>()
            .AddSingleton<LoggingService>()
            .AddSingleton<YouTubeApiService>()
            .AddHostedService<ItemSyncService>()
            .AddHostedService<UserMigrationService>()
            .AddHostedService<DiscordBotWorker>();

        builder.Services.AddSingleton<AutoModFilterService>()
            .AddHostedService(provider => provider.GetRequiredService<AutoModFilterService>());

        builder.Services.AddSingleton<RaceSessionService>()
            .AddSingleton<RaceEngine>();
    }
}
