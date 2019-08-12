using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Core;
using System;
using System.IO;
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
            _client.MessageReceived += HandleCommandAsync;
        }

        public string Name { get; }

        private async Task HandleCommandAsync(SocketMessage s)
        {
            if (!(s is SocketUserMessage msg)) return;

            var context = new SocketCommandContext(_client, msg);

            int argPos = 0;

            var server = new MBServer(0);

            if (!context.IsPrivate) 
            {
                if (Global.Servers.Value.Any(sr => sr.Id == context.Guild.Id))
                    server =  MarbleBotModule.GetServer(context);
                else {
                    server = new MBServer(context.Guild.Id);
                    Global.Servers.Value.Add(server);
                }
            }

            if (msg.HasStringPrefix("mb/", ref argPos) && msg.Author.IsBot == false && (context.IsPrivate || 
                server.UsableChannels.Count == 0 || server.UsableChannels.Contains(context.Channel.Id))) {
                var result = await _service.ExecuteAsync(context, argPos, null);

                if (!result.IsSuccess)
                {
                    switch (result.Error)
                    {
                        case CommandError.BadArgCount: await context.Channel.SendMessageAsync("Wrong number of arguments. Use `mb/help <command name>` to see how to use the command."); break;
                        case CommandError.UnknownCommand: break;
                        case CommandError.UnmetPrecondition: await context.Channel.SendMessageAsync("Insufficient permissions."); break;
                        default: await Program.Log($"{result.Error.Value}: {result.ErrorReason}"); break;
                    }
                }

            } else if (!context.IsPrivate && server.AutoresponseChannel == context.Channel.Id
                && DateTime.UtcNow.Subtract(Global.AutoresponseLastUse).TotalSeconds > 2) {
                var autoresponses = new System.Collections.Generic.List<string>();
                using (var autoresponseFile = new StreamReader($"Resources{Path.DirectorySeparatorChar}Autoresponses.txt"))
                {
                    while (!autoresponseFile.EndOfStream)
                        autoresponses.Add(autoresponseFile.ReadLine());
                }
                foreach (var response in autoresponses) {
                    var responseArray = response.Split(';');
                    if (string.Compare(context.Message.Content, responseArray[0], true) == 0) {
                        Global.AutoresponseLastUse = DateTime.UtcNow;
                        await context.Channel.SendMessageAsync(responseArray[1]); break;
                    }
                }
            }
            if (context.IsPrivate) await Program.Log($"{context.User}: {context.Message}");
        }
    }
}