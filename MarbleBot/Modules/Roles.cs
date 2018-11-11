using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace MarbleBot.Modules
{
    public class Roles : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Role-handling commands
        /// </summary>

        [Command("give")]
        [Summary("Gives a role")]
        public async Task _roleGive(string roleName)
        {
            await Context.Channel.TriggerTypingAsync();
            if (Context.IsPrivate) {
                await ReplyAsync("There are no roles in a DM!");
            } else {
                IRole role = Context.Guild.GetRole(237127439409610752);
                bool roleExists = true;
                switch (roleName.ToLower())
                {
                    case "roleplayer":
                        if (Context.Guild.Id == Global.THS)
                        {
                            role = Context.Guild.GetRole(242052397784891392);
                        }
                        break;
                    case "spammer":
                        if (Context.Guild.Id == Global.THS)
                        {
                            role = Context.Guild.GetRole(315212474909720577);
                        }
                        break;
                    case "dead":
                        if (Context.Guild.Id == Global.THS)
                        {
                            role = Context.Guild.GetRole(242048058580402177);
                        }
                        break;
                    case "archivist":
                        if (Context.Guild.Id == Global.THS)
                        {
                            role = Context.Guild.GetRole(339782998742532098);
                        }
                        break;
                    case "gamer":
                        if (Context.Guild.Id == Global.THS)
                        {
                            role = Context.Guild.GetRole(330036318895734786);
                        }
                        break;
                    case "algodoodlers":
                        if (Context.Guild.Id == Global.THS)
                        {
                            role = Context.Guild.GetRole(353958723548610561);
                        }
                        break;
                    case "bot commander":
                        if (Context.Guild.Id == Global.THS)
                        {
                            role = Context.Guild.GetRole(242048341247000577);
                        }
                        break;
                    case "spoiler":
                        if (Context.Guild.Id == Global.CM)
                        {
                            role = Context.Guild.GetRole(422479447686643712);
                        }
                        break;
                    case "spoilers":
                        if (Context.Guild.Id == Global.CM)
                        {
                            role = Context.Guild.GetRole(422479447686643712);
                        }
                        break;
                    default:
                        roleExists = false;
                        break;
                };
                if (roleExists)
                {
                    await (Context.User as IGuildUser).AddRoleAsync(role);
                    await Context.Channel.SendMessageAsync("Success. The **" + role.Name + "** role has been given to you.");
                }
                else
                {
                    await Context.Channel.SendMessageAsync("The requested role either does not exist or cannot be requested for. Make sure your spelling is correct!");
                }
            }
        }

        [Command("take")]
        [Summary("Takes a role from someone.")]
        public async Task _roleTake(string roleName)
        {
            await Context.Channel.TriggerTypingAsync();
            if (Context.IsPrivate)
            {
                await ReplyAsync("There are no roles in a DM!");
            }
            else
            {
                IRole role = Context.Guild.GetRole(237127439409610752);
                bool roleExists = true;
                switch (roleName.ToLower())
                {
                    case "roleplayer":
                        if (Context.Guild.Id == Global.THS)
                        {
                            role = Context.Guild.GetRole(242052397784891392);
                        }
                        break;
                    case "spammer":
                        if (Context.Guild.Id == Global.THS)
                        {
                            role = Context.Guild.GetRole(315212474909720577);
                        }
                        break;
                    case "dead":
                        if (Context.Guild.Id == Global.THS)
                        {
                            role = Context.Guild.GetRole(242048058580402177);
                        }
                        break;
                    case "archivist":
                        if (Context.Guild.Id == Global.THS)
                        {
                            role = Context.Guild.GetRole(339782998742532098);
                        }
                        break;
                    case "gamer":
                        if (Context.Guild.Id == Global.THS)
                        {
                            role = Context.Guild.GetRole(330036318895734786);
                        }
                        break;
                    case "algodoodlers":
                        if (Context.Guild.Id == Global.THS)
                        {
                            role = Context.Guild.GetRole(353958723548610561);
                        }
                        break;
                    case "bot commander":
                        if (Context.Guild.Id == Global.THS)
                        {
                            role = Context.Guild.GetRole(242048341247000577);
                        }
                        break;
                    case "spoiler":
                        if (Context.Guild.Id == Global.CM)
                        {
                            role = Context.Guild.GetRole(422479447686643712);
                        }
                        break;
                    case "spoilers":
                        if (Context.Guild.Id == Global.CM)
                        {
                            role = Context.Guild.GetRole(422479447686643712);
                        }
                        break;
                    default:
                        roleExists = false;
                        break;
                };
                if (roleExists)
                {
                    await (Context.User as IGuildUser).RemoveRoleAsync(role);
                    await Context.Channel.SendMessageAsync("Success. The **" + role.Name + "** role has been taken from you.");
                }
                else
                {
                    await Context.Channel.SendMessageAsync("The requested role either does not exist or cannot be requested for. Make sure your spelling is correct!");
                }
            }
        }

        [Command("rolelist")]
        [Summary("Shows a list of all roles")]
        public async Task _roleList()
        {
            await Context.Channel.TriggerTypingAsync();
            if (Context.IsPrivate)
            {
                await ReplyAsync("There are no roles in a DM!");
            }
            else
            {
                EmbedBuilder builder = new EmbedBuilder();
                switch (Context.Guild.Id)
                {
                    case Global.CM:
                        builder.AddField("MarbleBot Role List", "Spoilers")
                            .WithColor(Color.Teal)
                            .WithTimestamp(DateTime.UtcNow);
                        break;
                    case Global.THS:
                        builder.AddField("MarbleBot Role List", "Roleplayer\nGamer\nSpammer\nArchivist\nDead\nAlgodoodlers\nBot Commander")
                            .WithColor(Color.Orange)
                            .WithTimestamp(DateTime.UtcNow);
                        break;
                    case Global.THSC:
                        builder.AddField("MarbleBot Role List", "There aren't any roles here!")
                           .WithColor(Color.Orange)
                           .WithTimestamp(DateTime.UtcNow);
                        break;
                    case Global.MT:
                        builder.AddField("MarbleBot Role List", "There aren't any roles here!")
                           .WithColor(Color.DarkGrey)
                           .WithTimestamp(DateTime.UtcNow);
                        break;
                    case Global.VFC:
                        builder.AddField("MarbleBot Role List", "There aren't any roles here!")
                           .WithColor(Color.Blue)
                           .WithTimestamp(DateTime.UtcNow);
                        break;
                    default:
                        break;
                }
                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
        }
    }
}
