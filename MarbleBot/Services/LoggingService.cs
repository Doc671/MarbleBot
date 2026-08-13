using Microsoft.Extensions.DependencyInjection;
using NLog.Config;
using NLog.Targets;

namespace MarbleBot.Services;

public class LoggingService
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly DiscordSocketClient _client;

    public LoggingService(IServiceProvider serviceProvider)
    {
        SetLogConfig();

        _client = serviceProvider.GetRequiredService<DiscordSocketClient>();
    }

    public void Initialise()
    {
        _client.Log += Client_Log;
    }

    private static void SetLogConfig()
    {
        var config = new LoggingConfiguration();
        config.AddRule(LogLevel.Trace, LogLevel.Fatal, new FileTarget("logfile")
        {
            Layout = @"[${date:universalTime=True:format=yyyy-MM-dd HH\:mm\:ss}] ${message}",
            FileName = $"Logs{Path.DirectorySeparatorChar}MarbleBot-Logs-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.txt"
        });
        config.AddRule(LogLevel.Trace, LogLevel.Fatal, new ConsoleTarget("logconsole")
        {
            Layout = @"[${date:universalTime=True:format=yyyy-MM-dd HH\:mm\:ss}] ${message}"
        });
        LogManager.Configuration = config;
    }

    private Task Client_Log(LogMessage message)
    {
        if (message.Exception == null)
        {
            _logger.Info(message.Message);
        }
        else
        {
            _logger.Error(message.Message);
        }

        return Task.CompletedTask;
    }
}
