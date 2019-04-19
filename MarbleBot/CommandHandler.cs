using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Diagnostics;
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

            var IsLett = !char.TryParse(msg.Content.Trim('`'), out char e);
            if (!IsLett) IsLett = char.IsLetter(e) || e == '?' || e == '^' || char.IsNumber(e);

            if (msg.HasStringPrefix("mb/", ref argPos) && msg.Author.IsBot == false && (Global.UsableChannels.Any(chnl => chnl == context.Channel.Id) || context.IsPrivate)) {
                var result = await _service.ExecuteAsync(context, argPos, null);
                
                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                    await Program.Log($"{result.Error.Value}: {result.ErrorReason}");
                if (result.Error == CommandError.BadArgCount) await context.Channel.SendMessageAsync("Wrong number of arguments. Use `mb/help <command name>` to see how to use the command.");

            } else if (!(IsLett) && context.Channel.Id != 252481530130202624 && (context.Guild.Id == 224277738608001024 || context.Guild.Id == 223616088263491595)) {
                EmbedBuilder builder = new EmbedBuilder()
                    .WithAuthor(context.User)
                    .WithDescription($"**Message sent by { context.User.Mention} deleted in <#{context.Channel.Id }>** {context.Message.Content}")
                    .AddField("Reason", "Empty Message")
                    .WithColor(Color.Red)
                    .WithFooter("ID: " + context.User.Id)
                    .WithCurrentTimestamp();
                if (context.Guild.Id == 224277738608001024) {
                    var logs = context.Guild.GetTextChannel(327132239257272327);
                    await logs.SendMessageAsync("", false, builder.Build());
                } else if (context.Guild.Id == 223616088263491595) {
                    var logs = context.Guild.GetTextChannel(387306347936350213);
                    await logs.SendMessageAsync("", false, builder.Build());
                }
                await context.Message.DeleteAsync();
            } else if (context.Channel.Id == 252481530130202624 && DateTime.UtcNow.Subtract(Global.ARLastUse).TotalSeconds > 2) {
                foreach (var response in Global.Autoresponses) {
                    if (context.Message.Content.ToLower() == response.Key) {
                        Global.ARLastUse = DateTime.UtcNow;
                        await context.Channel.SendMessageAsync(response.Value); break;
                    }
                }
            }
            if (context.IsPrivate) await Program.Log($"{context.User}: {context.Message}");
        }
    }
}