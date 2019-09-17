using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Core;
using System;
using System.Linq;
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

        public string Name { get; }

        private async Task HandleCommand(SocketMessage s)
        {
            if (!(s is SocketUserMessage msg)) return;

            var context = new SocketCommandContext(_client, msg);

            int argPos = 0;

            var guild = new MarbleBotGuild(0);

            if (!context.IsPrivate)
            {
                if (Global.Servers.Any(sr => sr.Id == context.Guild.Id))
                    guild = MarbleBotModule.GetGuild(context);
                else
                {
                    guild = new MarbleBotGuild(context.Guild.Id);
                    Global.Servers.Add(guild);
                }
            }


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
                        case CommandError.UnmetPrecondition:
                            await context.Channel.SendMessageAsync("Insufficient permissions.");
                            break;
                        case CommandError.UnknownCommand:
                            await context.Channel.SendMessageAsync("Unknown command. Use `mb/help` to see what commands there are.");
                            break;
                        default:
                            Program.Log($"{result.Error.Value}: {result.ErrorReason}");
                            await context.Channel.TriggerTypingAsync();
                            break;
                    }
                }

            }
            else if (!context.IsPrivate && guild.AutoresponseChannel == context.Channel.Id
              && DateTime.UtcNow.Subtract(Global.AutoresponseLastUse).TotalSeconds > 2)
            {
                foreach (var response in Global.Autoresponses)
                {
                    if (string.Compare(context.Message.Content, response.Key, true) == 0)
                    {
                        Global.AutoresponseLastUse = DateTime.UtcNow;
                        await context.Channel.SendMessageAsync(response.Value);
                        break;
                    }
                }
            }
            if (context.IsPrivate) Program.Log($"{context.User}: {context.Message}");
        }
    }
}