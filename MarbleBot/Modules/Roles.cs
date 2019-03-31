﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace MarbleBot.Modules
{        
    /// <summary> Role-handling commands. </summary>
    public class Roles : ModuleBase<SocketCommandContext>
    {
        [Command("give")]
        [Alias("giverole")]
        [Summary("Gives a role if it is on the role list.")]
        public async Task GiveRoleCommandAsync(string roleName)
        {
            await Context.Channel.TriggerTypingAsync();
            if (Context.IsPrivate) await ReplyAsync("There are no roles in a DM!");
            else
            {
                IRole role = Context.Guild.GetRole(237127439409610752);
                bool roleExists = true;
                switch (roleName.ToLower())
                {
                    case "roleplayer": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(242052397784891392); break;
                    case "spammer": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(315212474909720577); break;
                    case "dead": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(242048058580402177); break;
                    case "archivist": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(339782998742532098); break;
                    case "gamer": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(330036318895734786); break;
                    case "algodoodlers": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(353958723548610561); break;
                    case "bot commander": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(242048341247000577); break;
                    case "spoiler": if (Context.Guild.Id == Global.CM) role = Context.Guild.GetRole(422479447686643712); break;
                    case "spoilers": if (Context.Guild.Id == Global.CM) role = Context.Guild.GetRole(422479447686643712); break;
                    default: roleExists = false; break;
                };
                if (roleExists)
                {
                    await (Context.User as IGuildUser).AddRoleAsync(role);
                    await Context.Channel.SendMessageAsync("Success. The **" + role.Name + "** role has been given to you.");
                }
                else await Context.Channel.SendMessageAsync("The requested role either does not exist or cannot be requested for. Make sure your spelling is correct!");
            }
        }

        [Command("take")]
        [Alias("removerole, takerole")]
        [Summary("Takes a role if it is on the role list.")]
        public async Task TakeRoleCommandAsync(string roleName)
        {
            await Context.Channel.TriggerTypingAsync();
            if (Context.IsPrivate) await ReplyAsync("There are no roles in a DM!");
            else
            {
                IRole role = Context.Guild.GetRole(237127439409610752);
                bool roleExists = true;
                switch (roleName.ToLower())
                {
                    case "roleplayer": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(242052397784891392); break;
                    case "spammer": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(315212474909720577); break;
                    case "dead": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(242048058580402177); break;
                    case "archivist": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(339782998742532098); break;
                    case "gamer": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(330036318895734786); break;
                    case "algodoodlers": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(353958723548610561); break;
                    case "bot commander": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(242048341247000577); break;
                    case "spoiler": if (Context.Guild.Id == Global.CM) role = Context.Guild.GetRole(422479447686643712); break;
                    case "spoilers": if (Context.Guild.Id == Global.CM) role = Context.Guild.GetRole(422479447686643712); break;
                    default: roleExists = false; break;
                };
                if (roleExists)
                {
                    await (Context.User as IGuildUser).RemoveRoleAsync(role);
                    await Context.Channel.SendMessageAsync("Success. The **" + role.Name + "** role has been taken from you.");
                }
                else await Context.Channel.SendMessageAsync("The requested role either does not exist or cannot be requested for. Make sure your spelling is correct!");
            }
        }

        [Command("role")]
        [Alias("roletoggle")]
        [Summary("Toggles role list roles.")]
        public async Task RoleToggleCommandAsync(string roleName)
        {
            await Context.Channel.TriggerTypingAsync();
            if (Context.IsPrivate) await ReplyAsync("There are no roles in a DM!");
            else
            {
                IRole role = Context.Guild.GetRole(237127439409610752);
                bool roleExists = true;
                SocketGuildUser user = Context.Guild.GetUser(Context.User.Id);
                switch (roleName.ToLower()) {
                    case "roleplayer": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(242052397784891392); break;
                    case "spammer": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(315212474909720577); break;
                    case "dead": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(242048058580402177); break;
                    case "archivist": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(339782998742532098); break;
                    case "gamer": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(330036318895734786); break;
                    case "algodoodlers": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(353958723548610561); break;
                    case "bot commander": if (Context.Guild.Id == Global.THS) role = Context.Guild.GetRole(242048341247000577); break;
                    case "spoiler": if (Context.Guild.Id == Global.CM) role = Context.Guild.GetRole(422479447686643712); break;
                    case "spoilers": if (Context.Guild.Id == Global.CM) role = Context.Guild.GetRole(422479447686643712); break;
                    default: roleExists = false; break;
                };
                if (roleExists) {
                    bool hasRole = false;
                    foreach (SocketRole rol in user.Roles) {
                        if (rol.Name.ToLower() == roleName.ToLower()) hasRole = true;
                    }
                    if (hasRole) {
                        await (Context.User as IGuildUser).RemoveRoleAsync(role);
                        await Context.Channel.SendMessageAsync("Success. The **" + role.Name + "** role has been taken from you.");
                    } else {
                        await (Context.User as IGuildUser).AddRoleAsync(role);
                        await Context.Channel.SendMessageAsync("Success. The **" + role.Name + "** role has been given to you.");
                    }
                }
                else await Context.Channel.SendMessageAsync("The requested role either does not exist or cannot be requested for. Make sure your spelling is correct!");
            }
        }

        [Command("rolelist")]
        [Alias("roles")]
        [Summary("Shows a list of roles that can be given/taken by `mb/give` and `mb/take`.")]
        public async Task RoleListCommandAsync()
        {
            await Context.Channel.TriggerTypingAsync();
            if (Context.IsPrivate) {
                await ReplyAsync("There are no roles in a DM!");
            }
            else {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithColor(Global.GetColor(Context));
                switch (Context.Guild.Id) {
                    case Global.CM:
                        builder.AddField("MarbleBot Role List", "Spoilers")
                            .WithTimestamp(DateTime.UtcNow);
                        break;
                    case Global.THS:
                        builder.AddField("MarbleBot Role List", "Roleplayer\nGamer\nSpammer\nArchivist\nDead\nAlgodoodlers\nBot Commander")
                            .WithTimestamp(DateTime.UtcNow);
                        break;
                    case Global.THSC:
                        builder.AddField("MarbleBot Role List", "There aren't any roles here!")
                           .WithTimestamp(DateTime.UtcNow);
                        break;
                    case Global.MT:
                        builder.AddField("MarbleBot Role List", "There aren't any roles here!")
                           .WithTimestamp(DateTime.UtcNow);
                        break;
                    case Global.VFC:
                        builder.AddField("MarbleBot Role List", "There aren't any roles here!")
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
