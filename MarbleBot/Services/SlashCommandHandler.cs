using MarbleBot.Entities;
using MarbleBot.Modules.Converters;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MarbleBot.Services;

public class SlashCommandHandler
{
    private readonly DiscordSocketClient _client;
    private readonly BotCredentials _credentials;
    private readonly InteractionService _interactionService;
    private readonly ILogger<SlashCommandHandler> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly GuildUsableChannelProvider _guildUsableChannelProvider;

    public SlashCommandHandler(DiscordSocketClient client, BotCredentials credentials, InteractionService interactionService, ILogger<SlashCommandHandler> logger, IServiceProvider serviceProvider, GuildUsableChannelProvider guildUsableChannelProvider)
    {
        _client = client;
        _credentials = credentials;
        _interactionService = interactionService;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _guildUsableChannelProvider = guildUsableChannelProvider;
    }

    public async Task Initialise()
    {
        _interactionService.Log += LogAsync;

        _interactionService.AddTypeConverter<User>(new UserTypeConverter());

        await _interactionService.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider);

        _client.Ready += SetupCommandsAsync;
        _client.InteractionCreated += HandleInteractionAsync;
    }

    private Task LogAsync(LogMessage logMessage)
    {
        var logLevel = logMessage.Severity switch
        {
            LogSeverity.Critical => Microsoft.Extensions.Logging.LogLevel.Critical,
            LogSeverity.Error => Microsoft.Extensions.Logging.LogLevel.Error,
            LogSeverity.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
            LogSeverity.Info => Microsoft.Extensions.Logging.LogLevel.Information,
            LogSeverity.Verbose => Microsoft.Extensions.Logging.LogLevel.Debug,
            LogSeverity.Debug => Microsoft.Extensions.Logging.LogLevel.Trace,
            _ => Microsoft.Extensions.Logging.LogLevel.Information
        };

        _logger.Log(logLevel, logMessage.Exception, "[{Source}] {Message}", logMessage.Source, logMessage.Message ?? logMessage.Exception?.Message);
        return Task.CompletedTask;
    }

    private async Task SetupCommandsAsync()
    {
#if DEBUG
        if (_credentials.DebugGuildIds != null && _credentials.DebugGuildIds.Length != 0)
        {
            foreach (ulong guildId in _credentials.DebugGuildIds)
            {
                _logger.LogInformation("Registering slash commands to guild with ID {GuildId} for debugging.", guildId);
                await _interactionService.RegisterCommandsToGuildAsync(guildId);
            }
        }
        else
        {
            _logger.LogInformation("No debug guild IDs provided. Registering slash commands globally. This may take up to an hour to update.");
            await _interactionService.RegisterCommandsGloballyAsync();
        }
#else
        await _interactionService.RegisterCommandsGloballyAsync();
#endif
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(_client, interaction);
            if (!await _guildUsableChannelProvider.IsChannelUsable(context))
            {
                await interaction.RespondAsync(":x: | Commands are blocked in this channel.", ephemeral: true);
                return;
            }

            var result = await _interactionService.ExecuteCommandAsync(context, _serviceProvider);
            if (!result.IsSuccess)
            {
                _logger.LogError("Error executing command: {ErrorReason}", result.ErrorReason);
                if (interaction.Type == InteractionType.ApplicationCommand)
                {
                    await interaction.RespondAsync(":x: | An error occurred while processing the command.", ephemeral: true);
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error handling interaction.");
            if (interaction.Type == InteractionType.ApplicationCommand)
            {
                await interaction.RespondAsync(":x: | An error occurred while processing the command.", ephemeral: true);
            }
        }
    }
}
