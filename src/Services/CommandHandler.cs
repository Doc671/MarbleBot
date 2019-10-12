using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Common;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace MarbleBot.Services
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly Logger _logger;
        private readonly IServiceProvider _services;

        public CommandHandler(IServiceProvider services)
        {
            _services = services;
            _client = _services.GetRequiredService<DiscordSocketClient>();
            _commands = _services.GetRequiredService<CommandService>();
            _logger = LogManager.GetCurrentClassLogger();
            _client.MessageReceived += HandleCommand;
        }

        public async Task InitializeAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services).ConfigureAwait(false);
        }

        private async Task HandleCommand(SocketMessage msg)
        {
            if (msg.Author.IsBot)
                return;

            if (!(msg is SocketUserMessage userMsg))
                return;

            var context = new SocketCommandContext(_client, userMsg);

            int argPos = 0;

            var guild = context.IsPrivate ?
                new MarbleBotGuild(id: 0)
                : Modules.MarbleBotModule.GetGuild(context);

            if (userMsg.HasStringPrefix(guild.Prefix, ref argPos) &&
#if DEBUG
            // If debugging, run commands in a single channel only
            context.Channel.Id == 409655798730326016)
#else
            // Otherwise, run as usual
            (context.IsPrivate || guild.UsableChannels.Count == 0 || guild.UsableChannels.Contains(context.Channel.Id)))
#endif
            {
                await context.Channel.TriggerTypingAsync();
                var result = await _commands.ExecuteAsync(context, argPos, _services);

                if (!result.IsSuccess)
                {
                    switch (result.Error)
                    {
                        case CommandError.BadArgCount:
                            await context.Channel.SendMessageAsync("Wrong number of arguments. Use `mb/help <command name>` to see how to use the command.");
                            break;
                        case CommandError.ParseFailed:
                            await context.Channel.SendMessageAsync("Failed to parse the given arguments. Use `mb/help <command name>` to see what type each argument should be.");
                            return;
                        case CommandError.UnmetPrecondition:
                            await context.Channel.SendMessageAsync("Insufficient permissions.");
                            break;
                        case CommandError.UnknownCommand:
                            await context.Channel.SendMessageAsync("Unknown command. Use `mb/help` to see what commands there are.");
                            break;
                        default:
                            await context.Channel.SendMessageAsync("An error has occured.");
                            _logger.Error($"{result.Error.Value}: {result.ErrorReason}");
                            break;
                    }
                }

            }
            else if (!context.IsPrivate && guild.AutoresponseChannel == context.Channel.Id)
            {
                var autoresponses = new Dictionary<string, string>();
                using (var autoresponseFile = new StreamReader($"Resources{Path.DirectorySeparatorChar}Autoresponses.txt"))
                {
                    while (!autoresponseFile.EndOfStream)
                    {
                        var autoresponsePair = (await autoresponseFile.ReadLineAsync()).Split(';');
                        autoresponses.Add(autoresponsePair[0], autoresponsePair[1]);
                    }
                }
                if (autoresponses.ContainsKey(context.Message.Content))
                    await context.Channel.SendMessageAsync(autoresponses[context.Message.Content]);
            }

            if (context.IsPrivate) _logger.Info("{0}: {1}", context.User, context.Message);
        }
    }
}