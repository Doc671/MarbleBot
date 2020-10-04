using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using MarbleBot.Common;
using MarbleBot.Modules.Games.Services;
using MarbleBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace MarbleBot
{
    public sealed class Program
    {
        private readonly BotCredentials _botCredentials;
        private readonly Logger _logger;
        private readonly StartTimeService _startTimeService = new StartTimeService(DateTime.UtcNow);

        private Program()
        {
            _botCredentials = GetBotCredentials();
            SetLogConfig();
            _logger = LogManager.GetCurrentClassLogger();
        }

        public static void Main()
        {
            new Program().StartAsync().GetAwaiter().GetResult();
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
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
        }

        private static BotCredentials GetBotCredentials()
        {
            string json;
            using (var botCredentialFile = new StreamReader("BotCredentials.json"))
            {
                json = botCredentialFile.ReadToEnd();
            }

            var returnValue = JsonConvert.DeserializeObject<BotCredentials>(json);
            using (FileStream? stream = File.Open($"Keys{Path.DirectorySeparatorChar}client_id.json", FileMode.Open,
                FileAccess.Read))
            {
                if (returnValue == null)
                {
                    throw new Exception("Bot credentials not detected.");
                }

                returnValue.GoogleUserCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets,
                    new[] { SheetsService.Scope.Spreadsheets },
                    "user",
                    CancellationToken.None,
                    new FileDataStore($"Keys{Path.DirectorySeparatorChar}token.json", true)
                ).Result;
            }

            return returnValue;
        }

        private static void SetLogConfig()
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
            LogManager.Configuration = config;
        }

        private async Task StartAsync()
        {
            Console.Title = "MarbleBot";

            await using ServiceProvider services = ConfigureServices();

            var client = services.GetRequiredService<DiscordSocketClient>();
            await client.LoginAsync(TokenType.Bot, _botCredentials.Token).ConfigureAwait(false);
            client.JoinedGuild += Client_JoinedGuild;
            client.LeftGuild += Client_LeftGuild;
            client.UserBanned += Client_UserBanned;
            _logger.Info("Logged in", DateTime.UtcNow);

            await client.StartAsync();

            await services.GetRequiredService<CommandHandler>().InitialiseAsync();

            await Task.Delay(-1);
        }

        private static Task Client_UserBanned(SocketUser user, SocketGuild socketGuild)
        {
            IDictionary<ulong, MarbleBotGuild> guildsDict = MarbleBotGuild.GetGuilds();
            if (!guildsDict.ContainsKey(socketGuild.Id))
            {
                return Task.CompletedTask;
            }

            MarbleBotGuild? marbleBotGuild = guildsDict[socketGuild.Id];
            if (string.IsNullOrEmpty(marbleBotGuild?.AppealFormLink))
            {
                return Task.CompletedTask;
            }

            user.SendMessageAsync($"You have been banned from {socketGuild.Name}. Use this appeal form if you would like to make an appeal: {marbleBotGuild.AppealFormLink}");
            return Task.CompletedTask;
        }

        private Task Client_LeftGuild(SocketGuild guild)
        {
            _logger.Info("Left guild {0} [{1}]", guild.Name, guild.Id);
            return Task.CompletedTask;
        }

        private Task Client_JoinedGuild(SocketGuild guild)
        {
            _logger.Info("Joined guild {0} [{1}]", guild.Name, guild.Id);
            return Task.CompletedTask;
        }
    }
}
