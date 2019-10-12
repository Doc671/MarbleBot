using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Modules
{
    /// <summary> Role-handling commands. </summary>
    public class Roles : MarbleBotModule
    {
        [Command("give")]
        [Alias("giverole")]
        [Summary("Gives a role if it is on the role list.")]
        [RequireContext(ContextType.Guild)]
        public async Task GiveRoleCommand([Remainder] string roleName)
        {
            if (Context.Guild.Roles.Any(r => string.Compare(r.Name, roleName, true) == 0))
            {
                var role = Context.Guild.Roles.Where(r => string.Compare(r.Name, roleName, true) == 0).First();
                var guild = GetGuild(Context);
                if (guild.Roles.Any(r => r == role.Id))
                {
                    await (Context.User as IGuildUser).AddRoleAsync(role);
                    await ReplyAsync($"Success. The **{role.Name}** role has been given to you.");
                    return;
                }
            }
            await SendErrorAsync("The requested role either does not exist or cannot be requested for. Make sure your spelling is correct!");
        }

        [Command("take")]
        [Alias("removerole, takerole")]
        [Summary("Takes a role if it is on the role list.")]
        [RequireContext(ContextType.Guild)]
        public async Task TakeRoleCommand([Remainder] string roleName)
        {
            if (Context.Guild.Roles.Any(r => string.Compare(r.Name, roleName, true) == 0))
            {
                var role = Context.Guild.Roles.Where(r => string.Compare(r.Name, roleName, true) == 0).First();
                var guild = GetGuild(Context);
                if (guild.Roles.Any(r => r == role.Id))
                {
                    await (Context.User as IGuildUser).RemoveRoleAsync(role);
                    await ReplyAsync($"Success. The **{role.Name}** role has been taken from you.");
                    return;
                }
            }
            await SendErrorAsync("The requested role either does not exist or cannot be requested for. Make sure your spelling is correct!");
        }

        [Command("role")]
        [Alias("roletoggle")]
        [Summary("Toggles role list roles.")]
        [RequireContext(ContextType.Guild)]
        public async Task RoleToggleCommand([Remainder] string roleName)
        {
            if (Context.Guild.Roles.Any(r => string.Compare(r.Name, roleName, true) == 0))
            {
                var role = Context.Guild.Roles.Where(r => string.Compare(r.Name, roleName, true) == 0).First();
                var guild = GetGuild(Context);
                if (guild.Roles.Any(r => r == role.Id))
                {
                    if ((Context.User as SocketGuildUser).Roles.Any(r => r.Id == role.Id))
                    {
                        await (Context.User as IGuildUser).RemoveRoleAsync(role);
                        await ReplyAsync($"Success. The **{role.Name}** role has been taken from you.");
                    }
                    else
                    {
                        await (Context.User as IGuildUser).AddRoleAsync(role);
                        await ReplyAsync($"Success. The **{role.Name}** role has been given to you.");
                    }
                    return;
                }
            }
            await SendErrorAsync("The requested role either does not exist or cannot be requested for. Make sure your spelling is correct!");
        }

        [Command("rolelist")]
        [Alias("roles")]
        [Summary("Shows a list of roles that can be given/taken by `mb/give` and `mb/take`.")]
        public async Task RoleListCommand()
        {
            var output = new StringBuilder();
            var guild = GetGuild(Context);
            foreach (var role in guild.Roles)
                output.AppendLine(Context.Guild.GetRole(role).Name);
            if (guild.Roles.Count < 1) output.Append("There aren't any roles here!");
            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithDescription(output.ToString())
                .WithTitle($"MarbleBot Role List: {Context.Guild.Name}")
                .Build());
        }
    }
}
