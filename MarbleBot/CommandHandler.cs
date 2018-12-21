using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace MarbleBot
{
    public class CommandHandler
    {
        private DiscordSocketClient _client;

        private CommandService _service;

        public CommandHandler(DiscordSocketClient client)
        {
            _client = client;

            _service = new CommandService();

            _service.AddModulesAsync(Assembly.GetEntryAssembly());

            _client.MessageReceived += HandleCommandAsync;
        }

        public string Name { get; }

        private async Task HandleCommandAsync(SocketMessage s)
        {
            var msg = s as SocketUserMessage;
            if (msg == null) return;

            var Context = new SocketCommandContext(_client, msg);

            await Context.Client.SetGameAsync("Try mb/help!");

            int argPos = 0;
            if (msg.HasStringPrefix("mb/", ref argPos) && msg.Author.IsBot == false) {
                var result = await _service.ExecuteAsync(Context, argPos);
                
                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                    Console.WriteLine("[" + DateTime.UtcNow + "] " + result.Error.Value + ": " + result.ErrorReason);
            } else if (msg.HasMentionPrefix(await Context.Channel.GetUserAsync(Global.BotId), ref argPos) && msg.Content.ToLower().Contains("no u")) {
                var msgs = await Context.Channel.GetMessagesAsync().Flatten();
                var noyour = false;
                foreach (var mesg in msgs) {
                    if (mesg.Content.ToLower().Contains("no your")) noyour = true;
                }
                if (noyour) await Context.Channel.SendMessageAsync(":warning: A No Your has been detected in the past 100 messages! The No U has been nullified!");
            }
            if (Context.IsPrivate) Console.WriteLine(string.Format("[{0}] {1}: {2}", DateTime.UtcNow, Context.User, Context.Message));
        }
    }
}
