using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Core;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace MarbleBot
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _service;

        public CommandHandler(DiscordSocketClient client)
        {
            _client = client;
            _service = new CommandService();
            _service.AddModulesAsync(Assembly.GetEntryAssembly(), null);
            Global.CommandService = _service;
            _client.MessageReceived += HandleCommand;
        }

        private async Task HandleCommand(SocketMessage s)
        {
            if (!(s is SocketUserMessage msg)) return;

            var context = new SocketCommandContext(_client, msg);

            int argPos = 0;

            var guild = new MarbleBotGuild(0);

            if (!context.IsPrivate)
                guild = Modules.MarbleBotModule.GetGuild(context);

            if (msg.HasStringPrefix("mb/", ref argPos) && !msg.Author.IsBot &&
#if DEBUG
            // If debugging, run commands in a single channel only
            context.Channel.Id == 409655798730326016)
#else
            // Otherwise, run as usual
            (context.IsPrivate || guild.UsableChannels.Count == 0 || guild.UsableChannels.Contains(context.Channel.Id)))
#endif
            {
                await context.Channel.TriggerTypingAsync();
                var result = await _service.ExecuteAsync(context, argPos, null);

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
                            Program.Log($"{result.Error.Value}: {result.ErrorReason}");
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
            if (context.IsPrivate) Program.Log($"{context.User}: {context.Message}");
        }
    }
}