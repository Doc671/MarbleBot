using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarbleBot.Services;

public class DiscordBotWorker : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly SlashCommandHandler _slashCommandHandler;
    private readonly LoggingService _loggingService;
    private readonly BotCredentials _credentials;
    private readonly ILogger<DiscordBotWorker> _logger;

    public DiscordBotWorker(DiscordSocketClient client, InteractionService interactionService, SlashCommandHandler slashCommandHandler, LoggingService loggingService, BotCredentials credentials, ILogger<DiscordBotWorker> logger)
    {
        _client = client;
        _interactionService = interactionService;
        _slashCommandHandler = slashCommandHandler;
        _loggingService = loggingService;
        _credentials = credentials;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting bot...");

        _loggingService.Initialise();
        await _slashCommandHandler.Initialise();

        await _client.LoginAsync(TokenType.Bot, _credentials.Token);
        await _client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping bot...");

        await _client.StopAsync();
        await _client.LogoutAsync();
    }
}
