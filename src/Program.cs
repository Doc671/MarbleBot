using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using MarbleBot.Modules.Games.Services;
using MarbleBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MarbleBot
{
    public sealed class Program
    {
        private readonly BotCredentials _botCredentials;
        private readonly NLog.Logger _logger;
        private StartTimeService _startTimeService;

        public static void Main()
            => new Program().StartAsync().GetAwaiter().GetResult();

        public Program()
        {
            _botCredentials = GetBotCredentials();
            SetLogConfig();
            _logger = NLog.LogManager.GetCurrentClassLogger();
        }

        private ServiceProvider ConfigureServices()
            => new ServiceCollection()
                .AddSingleton(_botCredentials)
                .AddSingleton(_startTimeService)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<GamesService>()
                .AddSingleton<DailyTimeoutService>()
                .AddSingleton<RandomService>()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                    loggingBuilder.AddNLog();
                })
                .BuildServiceProvider();

        private BotCredentials GetBotCredentials()
        {
            string json;
            using (var botCredentialFile = new StreamReader("BotCredentials.json"))
            {
                json = botCredentialFile.ReadToEnd();
            }

            var returnValue = JObject.Parse(json).ToObject<BotCredentials>();
            using (var stream = File.Open($"Keys{Path.DirectorySeparatorChar}client_id.json", FileMode.Open, FileAccess.Read))
            {
                returnValue.GoogleUserCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new string[] { SheetsService.Scope.Spreadsheets },
                    "user",
                    CancellationToken.None,
                    new FileDataStore($"Keys{Path.DirectorySeparatorChar}token.json", true)
                ).Result;
            }
            return returnValue;
        }

        private void SetLogConfig()
        {
            var config = new LoggingConfiguration();
            config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, new FileTarget("logfile")
            {
                Layout = @"[${date:universalTime=True:format=yyyy-MM-dd HH\:mm\:ss}] ${message}",
                FileName = $"Logs{Path.DirectorySeparatorChar}MarbleBot-Logs-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.txt"
            });
            config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, new ConsoleTarget("logconsole")
            {
                Layout = @"[${date:universalTime=True:format=yyyy-MM-dd HH\:mm\:ss}] ${message}"
            });
            NLog.LogManager.Configuration = config;
        }

        public async Task StartAsync()
        {
            Console.Title = "MarbleBot";
            _startTimeService = new StartTimeService(DateTime.UtcNow);

            using var services = ConfigureServices();

            var client = services.GetRequiredService<DiscordSocketClient>();
            await client.LoginAsync(TokenType.Bot, _botCredentials.Token).ConfigureAwait(false);
            client.JoinedGuild += Client_JoinedGuild;
            client.LeftGuild += Client_LeftGuild;
            _logger.Info("Logged in", DateTime.UtcNow);

            await client.StartAsync();

            await services.GetRequiredService<CommandHandler>().InitialiseAsync();

            await Task.Delay(-1);
        }

        private Task Client_LeftGuild(SocketGuild arg)
        {
            _logger.Info("Left guild {0} [{1}]", arg?.Name, arg?.Id);
            return Task.CompletedTask;
        }

        private Task Client_JoinedGuild(SocketGuild arg)
        {
            _logger.Info("Joined guild {0} [{1}]", arg?.Name, arg?.Id);
            return Task.CompletedTask;
        }
    }
}
