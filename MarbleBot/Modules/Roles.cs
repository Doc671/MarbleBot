using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace MarbleBot.Modules
{
    public class Roles : ModuleBase<SocketCommandContext>
    {
        const ulong CM = 223616088263491595; // Community Marble
        const ulong THS = 224277738608001024; // The Hat Stoar
        const ulong THSC = 318053169999511554; // The Hat Stoar Crew
        const ulong VFC = 394086559676235776; // Vinh Fan Club
        const ulong ABCD = 412253669392777217; // Blue & Ayumi's Discord Camp
        const ulong MT = 408694288604463114; // Melmon Test

        [Command("give")]
        [Summary("Gives a role")]
        public async Task _roleGive(string roleName)
        {
            IRole role = Context.Guild.GetRole(237127439409610752);
            bool roleExists = true;
            switch (roleName.ToLower()) {
                case "roleplayer":
                    if (Context.Guild.Id == THS) {
                        role = Context.Guild.GetRole(242052397784891392);
                    }
                    break;
                case "spammer":
                    if (Context.Guild.Id == THS) {
                        role = Context.Guild.GetRole(315212474909720577);
                    }
                    break;
                case "dead":
                    if (Context.Guild.Id == THS) {
                        role = Context.Guild.GetRole(242048058580402177);
                    }
                    break;
                case "archivist":
                    if (Context.Guild.Id == THS) {
                        role = Context.Guild.GetRole(339782998742532098);
                    }
                    break;
                case "gamer":
                    if (Context.Guild.Id == THS) {
                        role = Context.Guild.GetRole(330036318895734786);
                    }
                    break;
                case "algodoodlers":
                    if (Context.Guild.Id == THS) {
                        role = Context.Guild.GetRole(353958723548610561);
                    }
                    break;
                case "spoiler":
                    if (Context.Guild.Id == CM) {
                        role = Context.Guild.GetRole(422479447686643712);
                    }
                    break;
                case "spoilers":
                    if (Context.Guild.Id == CM) {
                        role = Context.Guild.GetRole(422479447686643712);
                    }
                    break;
                default:
                    roleExists = false;
                    break;
            };
            if (roleExists) {
                await (Context.User as IGuildUser).AddRoleAsync(role);
                await Context.Channel.SendMessageAsync("Success. The **" + role.Name + "** role has been given to you.");
            } else {
                await Context.Channel.SendMessageAsync("The requested role either does not exist or cannot be requested for. Make sure your spelling is correct!");
            }
        }

        [Command("take")]
        [Summary("Takes a role from someone.")]
        public async Task _roleTake(string roleName)
        {
            IRole role = Context.Guild.GetRole(237127439409610752);
            bool roleExists = true;
            switch (roleName.ToLower()) {
                case "roleplayer":
                    if (Context.Guild.Id == THS) {
                        role = Context.Guild.GetRole(242052397784891392);
                    }
                    break;
                case "spammer":
                    if (Context.Guild.Id == THS) {
                        role = Context.Guild.GetRole(315212474909720577);
                    }
                    break;
                case "dead":
                    if (Context.Guild.Id == THS) {
                        role = Context.Guild.GetRole(242048058580402177);
                    }
                    break;
                case "archivist":
                    if (Context.Guild.Id == THS) {
                        role = Context.Guild.GetRole(339782998742532098);
                    }
                    break;
                case "gamer":
                    if (Context.Guild.Id == THS) {
                        role = Context.Guild.GetRole(330036318895734786);
                    }
                    break;
                case "algodoodlers":
                    if (Context.Guild.Id == THS) {
                        role = Context.Guild.GetRole(353958723548610561);
                    }
                    break;
                case "spoiler":
                    if (Context.Guild.Id == CM) {
                        role = Context.Guild.GetRole(422479447686643712);
                    }
                    break;
                case "spoilers":
                    if (Context.Guild.Id == CM) {
                        role = Context.Guild.GetRole(422479447686643712);
                    }
                    break;
                default:
                    roleExists = false;
                    break;
            };
            if (roleExists) {
                await (Context.User as IGuildUser).RemoveRoleAsync(role);
                await Context.Channel.SendMessageAsync("Success. The **" + role.Name + "** role has been taken from you.");
            } else {
                await Context.Channel.SendMessageAsync("The requested role either does not exist or cannot be requested for. Make sure your spelling is correct!");
            }
        }

        [Command("rolelist")]
        [Summary("Shows a list of all roles")]
        public async Task _roleList()
        {
            EmbedBuilder builder = new EmbedBuilder();
            switch (Context.Guild.Id) {
                case CM:
                    builder.AddField("MarbleBot Role List", "Spoilers")
                        .WithColor(Color.Teal)
                        .WithTimestamp(DateTime.UtcNow);
                    break;
                case THS:
                    builder.AddField("MarbleBot Role List", "Roleplayer\nGamer\nSpammer\nArchivist\nDead\nAlgodoodlers")
                        .WithColor(Color.Orange)
                        .WithTimestamp(DateTime.UtcNow);
                    break;
                case THSC:
                    builder.AddField("MarbleBot Role List", "There aren't any roles here!")
                       .WithColor(Color.Orange)
                       .WithTimestamp(DateTime.UtcNow);
                    break;
                case MT:
                    builder.AddField("MarbleBot Role List", "There aren't any roles here!")
                       .WithColor(Color.DarkGrey)
                       .WithTimestamp(DateTime.UtcNow);
                    break;
                case VFC:
                    builder.AddField("MarbleBot Role List", "There aren't any roles here!")
                       .WithColor(Color.Blue)
                       .WithTimestamp(DateTime.UtcNow);
                    break;
                case ABCD:
                    builder.AddField("MarbleBot Role List", "There aren't any roles here!")
                       .WithColor(Color.Gold)
                       .WithTimestamp(DateTime.UtcNow);
                    break;
                default:
                    break;
            }
            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }
    }
}
