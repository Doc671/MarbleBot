using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

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

            _client.MessageReceived += HandleCommandAsync;
        }

        public string Name { get; }

        private async Task HandleCommandAsync(SocketMessage s)
        {
            if (!(s is SocketUserMessage msg)) return;

            var Context = new SocketCommandContext(_client, msg);

            await Context.Client.SetGameAsync("Try mb/help!");

            int argPos = 0;

            var IsLett = !char.TryParse(msg.Content.Trim('`'), out char e);
            if (!IsLett) IsLett = char.IsLetter(e) || e == '?' || e == '^' || char.IsNumber(e);

            if (msg.HasStringPrefix("mb/", ref argPos) && msg.Author.IsBot == false && (Global.UsableChannels.Any(chnl => chnl == Context.Channel.Id) || Context.IsPrivate)) {
                var result = await _service.ExecuteAsync(Context, argPos, null);
                
                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                    Trace.WriteLine($"[{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}] {result.Error.Value}: {result.ErrorReason}");
            } else if (msg.HasMentionPrefix(await Context.Channel.GetUserAsync(Global.BotId), ref argPos) && msg.Content.ToLower().Contains("no u")) {
                var msgs = await Context.Channel.GetMessagesAsync().FlattenAsync();
                foreach (var mesg in msgs) if (mesg.Content.ToLower().Contains("no your")) await Context.Channel.SendMessageAsync(":warning: A No Your has been detected in the past 100 messages! The No U has been nullified!");

            } else if (!(IsLett) && Context.Channel.Id != 252481530130202624 && (Context.Guild.Id == Global.THS || Context.Guild.Id == Global.CM)) {
                EmbedBuilder builder = new EmbedBuilder()
                    .WithAuthor(Context.User)
                    .WithDescription(string.Format("**Message sent by {0} deleted in {1}**\n{2}", Context.User.Mention, "<#" + Context.Channel.Id + ">", Context.Message.Content))
                    .AddField("Reason", "Empty Message")
                    .WithColor(Color.Red)
                    .WithFooter("ID: " + Context.User.Id)
                    .WithCurrentTimestamp();
                if (Context.Guild.Id == Global.THS) {
                    var logs = Context.Guild.GetTextChannel(327132239257272327);
                    await logs.SendMessageAsync("", false, builder.Build());
                } else if (Context.Guild.Id == Global.CM) {
                    var logs = Context.Guild.GetTextChannel(387306347936350213);
                    await logs.SendMessageAsync("", false, builder.Build());
                }
                await Context.Message.DeleteAsync();
            } else if (Context.Channel.Id == 252481530130202624 && DateTime.UtcNow.Subtract(Global.ARLastUse).TotalSeconds > 2) {
                foreach (var response in Global.Autoresponses) {
                    if (Context.Message.Content.ToLower() == response.Key) {
                        Global.ARLastUse = DateTime.UtcNow;
                        await Context.Channel.SendMessageAsync(response.Value); break;
                    }
                }
            }
            if (Context.IsPrivate) Trace.WriteLine(string.Format("[{0}] {1}: {2}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), Context.User, Context.Message));
        }
    }
}
