using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Modules
{
    class Moderation : ModuleBase<SocketCommandContext>
    {
        [Command("clear")]
        [Summary("Deletes the specified amount of messages.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task _clear(uint amount)
        {
            var messages = await Context.Channel.GetMessagesAsync((int)amount + 1).Flatten();
            await Context.Channel.DeleteMessagesAsync(messages);
            const int delay = 5000;
            var m = await ReplyAsync($"{amount} message(s) have been deleted. This message will be deleted in {delay / 1000} seconds.");
            await Task.Delay(delay);
            await m.DeleteAsync();
        }
    }
}
