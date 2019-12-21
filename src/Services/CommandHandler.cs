﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Common;
using MarbleBot.Common.TypeReaders;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MarbleBot.Services
{
    public class CommandHandler
    {
        private readonly BotCredentials _botCredentials;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly Logger _logger;
        private readonly IServiceProvider _services;

        public CommandHandler(IServiceProvider services)
        {
            _services = services;
            _botCredentials = _services.GetRequiredService<BotCredentials>();
            _client = _services.GetRequiredService<DiscordSocketClient>();
            _commands = _services.GetRequiredService<CommandService>();
            _logger = LogManager.GetCurrentClassLogger();
        }

        public async Task InitialiseAsync()
        {
            _client.MessageReceived += HandleCommandAsync;
            _commands.CommandExecuted += OnCommandExecutedAsync;

            _commands.AddTypeReader<Item>(new ItemTypeReader());
            _commands.AddTypeReader<MarbleBotUser>(new MarbleBotUserTypeReader());
            _commands.AddTypeReader<Weapon>(new WeaponTypeReader());

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services).ConfigureAwait(false);

            await _client.SetGameAsync("for mb/help!", type: ActivityType.Watching);
        }

        private async Task HandleCommandAsync(SocketMessage msg)
        {
            if (msg.Author.IsBot || !(msg is SocketUserMessage userMsg))
            {
                return;
            }

            var context = new SocketCommandContext(_client, userMsg);

            int argPos = 0;

            var guild = context.IsPrivate ?
                new MarbleBotGuild(id: 0)
                : Modules.MarbleBotModule.GetGuild(context);

            if (userMsg.HasStringPrefix(guild.Prefix, ref argPos) &&
#if DEBUG
            // If debugging, run commands in a single channel only
            context.Channel.Id == _botCredentials.DebugChannel || context.IsPrivate && _botCredentials.AdminIds.Any(id => id == (context.Channel as IDMChannel)!.Recipient.Id))
#else
            // Otherwise, run as usual
            (context.IsPrivate || guild.UsableChannels.Count == 0 || guild.UsableChannels.Contains(context.Channel.Id)))
#endif
            {
                await context.Channel.TriggerTypingAsync();
                await _commands.ExecuteAsync(context, argPos, _services);
            }
            else if (!context.IsPrivate && guild.AutoresponseChannel == context.Channel.Id)
            {
                var autoresponses = new Dictionary<string, string>();
                using (var autoresponseFile = new StreamReader($"Resources{Path.DirectorySeparatorChar}Autoresponses.txt"))
                {
                    while (!autoresponseFile.EndOfStream)
                    {
                        var autoresponsePair = (await autoresponseFile.ReadLineAsync())!.Split(';');
                        if (autoresponsePair != null)
                        {
                            autoresponses.Add(autoresponsePair[0], autoresponsePair[1]);
                        }
                    }
                }
                if (autoresponses.ContainsKey(context.Message.Content))
                {
                    await context.Channel.SendMessageAsync(autoresponses[context.Message.Content]);
                }
            }

            if (context.IsPrivate)
            {
                _logger.Info("{0}: {1}", context.User, context.Message);
            }
        }

        private async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!result.IsSuccess)
            {
                switch (result.Error)
                {
                    case CommandError.BadArgCount:
                        await context.Channel.SendMessageAsync(":warning: | Wrong number of arguments. Use `mb/help <command name>` to see how to use the command.");
                        break;
                    case CommandError.ParseFailed:
                        await context.Channel.SendMessageAsync(":warning: | Failed to parse the given arguments. Use `mb/help <command name>` to see what type each argument should be.");
                        break;
                    case CommandError.UnmetPrecondition:
                        await context.Channel.SendMessageAsync(":warning: | Insufficient permissions.");
                        break;
                    case CommandError.UnknownCommand:
                        await context.Channel.SendMessageAsync(":warning: | Unknown command. Use `mb/help` to see what commands there are.");
                        break;
                    default:
                        await context.Channel.SendMessageAsync($":warning: | An error has occured. ```{result.ErrorReason}```");
                        _logger.Error($"{result.Error!.Value}: {result.ErrorReason}");
                        break;
                }
            }
        }
    }
}
