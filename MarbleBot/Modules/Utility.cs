using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.BaseClasses;
using MarbleBot.Extensions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Modules
{
    /// <summary> Utility commands. </summary>
    public class Utility : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        [Alias("cmds")]
        [Summary("Gives the user help.")]
        public async Task HelpCommandAsync([Remainder] string command = "")
        {
            await Context.Channel.TriggerTypingAsync();
            var builder = new EmbedBuilder()
                .WithCurrentTimestamp()
                .WithColor(Global.GetColor(Context));
            switch (command.ToLower()) {
                case "":
                    builder.AddField("Modules", "Economy\nFun\nGames\nRoles\nUtility\nYouTube")
                        .WithDescription("*by Doc671#1965*\n\nUse mb/help followed by the name of a module or a command for more info, or mb/help full for the old help menu.")
                        .WithTitle("MarbleBot Help");
                    await ReplyAsync(embed: builder.Build());
                    break;
                case "economy":
                    var allCmds = new StringBuilder();
                    var commands = Global.CommandService.Commands.Where(c => c.Module.Name.ToLower() == command.ToLower());
                    if (!Context.IsPrivate) {
                        if (Context.Guild.Id == Global.CM) commands = commands.Where(c => c.Remarks != "Not CM");
                        else commands = commands.Where(c => c.Remarks != "CM Only");
                    } else commands = commands.Where(c => c.Remarks != "Not DMs" || c.Remarks != "CM Only");
                    foreach (var cmd in commands) allCmds.AppendLine($"**{cmd.Name}** - {cmd.Summary}");
                    builder.AddField($"{Global.CommandService.Modules.Where(m => m.Name.ToLower() == command).First().Name} Commands", allCmds.ToString())
                        .WithDescription("*by Doc671#1965*")
                        .WithTitle("MarbleBot Help");
                    await ReplyAsync(embed: builder.Build());
                    break;
                case "fun": goto case "economy";
                case "games": goto case "economy";
                case "roles": goto case "economy";
                case "utility": goto case "economy";
                case "youtube": goto case "economy";
                case "yt":
                    command = "youtube";
                    goto case "economy";
                case "full": 
                    builder.AddField("MarbleBot Help", "*by Doc671#1965*\nUse mb/help followed by a command name for more info!")
                        .WithTimestamp(DateTime.UtcNow);
                    if (Context.IsPrivate) {
                        builder.AddField("Fun Commands", "\n7ball (predicts an outcome)\nbet (bets on a marble out of a chosen number)\nbuyhat (buys an Uglee Hat)\nchoose (chooses between options split with '|')\norange (gives a random statement in Orange Language)\norangeify (turns a message you say into Orange Language)\nrate (rates something out of 10)\nrandom (returns a random positive integer with defined bounds)\nrank (shows your level and total XP)\nrepeat (repeats a message you say)\nvinhglish (shows the meaning and inventor of a Vinhglish word)")
                        .AddField("Economy Commands", "balance (returns how much money you or someone else has)\nbuy (buy an item)\ndaily (gives daily money)\nitem (view item info)\npoupsoop (calculates price total)\nprofile (returns profile of you or someone else)\nrichlist (shows 10 richest people)\nsell (sell an item)\nshop (view all items)")
                        .AddField("Utility Commands", "help (gives command help)\nuptime (shows how long the bot has been running)")
                        .AddField("YouTube Commands", "searchchannel (searches for a channel)\nsearchvideo (searches for a video)")
                        .AddField("Games", "\nrace (participate in a marble race)\nsiege (participate in a Marble Siege)")
                        .WithColor(Color.DarkerGrey);
                    } else {
                        builder.WithColor(Global.GetColor(Context));
                        switch (Context.Guild.Id) {
                            case Global.CM:
                                builder.AddField("Fun Commands", "7ball (predicts an outcome)\nbest (picks a random user to call the best)\nbet (bets on a marble out of a chosen number)\nchoose (chooses between options split with '|')\nrate (rates something out of 10)\nrandom (returns a random positive integer with defined bounds)\nrank (shows your level and total XP)\nrepeat (repeats a message you say)\nreverse (reverses text)")
                                    .AddField("Economy Commands", "balance (returns how much money you or someone else has)\nbuy (buy an item)\ndaily (gives daily money)\nitem (view item info)\npoupsoop (calculates price total)\nprofile (returns profile of you or someone else)\nrichlist (shows 10 richest people)\nsell (sell an item)\nshop (view all items)")
                                    .AddField("Utility Commands", "help (gives command help)\nserverinfo (displays information about the server)\nstaffcheck (checks the statuses of all staff members)\nuptime (shows how long the bot has been running)\nuserinfo (displays information about a user)")
                                    .AddField("Role Commands", "give (gives a role)\ntake (takes a role)\nrolelist (lists all roles that can be given/taken)")
                                    .AddField("YouTube Commands", "searchchannel (searches for a channel)\nsearchvideo (searches for a video)")
                                    .AddField("Games", "\nrace (participate in a marble race)\nsiege (participate in a Marble Siege)");
                                break;
                            default:
                                builder.AddField("Fun Commands", "\n7ball (predicts an outcome)\nbest (picks a random user to call the best)\nbet (bets on a marble out of a chosen number)\nbuyhat (buys an Uglee Hat)\nchoose (chooses between options split with '|')\norange (gives a random statement in Orange Language)\norangeify (turns a message you say into Orange Language)\nrate (rates something out of 10)\nrandom (returns a random positive integer with defined bounds)\nrank (shows your level and total XP)\nrepeat (repeats a message you say)\nstaffcheck (checks the statuses of all staff members)\nvinhglish (shows the meaning and inventor of a Vinhglish word)")
                                    .AddField("Economy Commands", "balance (returns how much money you or someone else has)\nbuy (buy an item)\ndaily (gives daily money)\nitem (view item info)\npoupsoop (calculates price total)\nprofile (returns profile of you or someone else)\nrichlist (shows 10 richest people)\nsell (sell an item)\nshop (view all items)")
                                    .AddField("Utility Commands", "help (gives command help)\nserverinfo (displays information about the server)\nstaffcheck (checks the statuses of all staff members)\nuptime (shows how long the bot has been running)\nuserinfo (displays information about a user)")
                                    .AddField("Role Commands", "give (gives a role)\ntake (takes a role)\nrolelist (lists all roles that can be given/taken)")
                                    .AddField("YouTube Commands", "searchchannel (searches for a channel)\nsearchvideo (searches for a video)")
                                    .AddField("Games", "\nrace (participate in a marble race)\nsiege (participate in a Marble Siege)");
                                break;
                        }
                    }
                    await ReplyAsync(embed: builder.Build());
                    break;
                default:
                    MBCommand bCommand = new MBCommand();
                    string THSOnly = "This command cannot be used in Community Marble!";
                    bCommand.Name = command;
                    switch (command) {
                        // General
                        case "7ball": bCommand.Desc = "Predicts an outcome to an event."; bCommand.Usage = "mb/7ball <condition>"; bCommand.Example = "mb/7ball Will I break?"; break;
                        case "best": bCommand.Desc = "Picks a random user in the server to call the best."; bCommand.Usage = "mb/best"; break;
                        case "bet": bCommand.Desc = "Bets on a marble to win from a list of up to 100."; bCommand.Usage = "mb/bet [number of marbles]"; bCommand.Example = "mb/bet 30"; break;
                        case "buyhat": bCommand.Desc = "Picks a random user in the server to call the best."; bCommand.Usage = "mb/buyhat"; bCommand.Warning = THSOnly; break;
                        case "choose": bCommand.Desc = "Chooses between several choices"; bCommand.Usage = "mb/choose <choice1> | <choice2>"; bCommand.Example = "Example: `mb/choose Red | Yellow | Green | Blue"; break;
                        case "orange": bCommand.Desc = "Gives a random statement in Orange Language."; bCommand.Usage = "mb/orange"; bCommand.Warning = THSOnly; break;
                        case "orangeify": bCommand.Desc = "Translates text into Orange Language."; bCommand.Usage = "mb/orangeify <text>"; bCommand.Example = "mb/orangeify Drink Poup Soop!"; bCommand.Warning = THSOnly; break;
                        case "random": bCommand.Desc = "Gives a random number between user-defined bounds."; bCommand.Usage = "mb/random <number1> <number2>"; bCommand.Example = "mb/random 1 5"; break;
                        case "rank": bCommand.Desc = "Returns the XP and level of the user."; bCommand.Usage = "mb/rank"; break;
                        case "rate": bCommand.Desc = "Rates something between 0 and 10."; bCommand.Usage = "mb/rate <text>"; bCommand.Example = "mb/rate Marbles"; break;
                        case "repeat": bCommand.Desc = "Repeats given text."; bCommand.Usage = "mb/repeat <text>"; bCommand.Example = "mb/repeat Hello!"; break;
                        case "reverse": bCommand.Desc = "Reverses text."; bCommand.Usage = "mb/reverse <text>"; bCommand.Example = "mb/reverse Bowl"; break;
                        case "vinhglish": bCommand.Desc = "Displays information about a Vinhglish word."; bCommand.Usage = "mb/vinglish OR mb/vinhglish <word>"; bCommand.Example = "mb/vinhglish Am Will You"; bCommand.Warning = THSOnly; break;

                        // Utility
                        case "serverinfo": bCommand.Desc = "Displays information about a server."; bCommand.Usage = "mb/serverinfo"; break;
                        case "staffcheck": bCommand.Desc = "Displays a list of all staff members and their statuses."; bCommand.Usage = "mb/staffcheck"; break;
                        case "uptime": bCommand.Desc = "Displays how long the bot has been running for."; bCommand.Usage = "mb/uptime"; break;
                        case "userinfo": bCommand.Desc = "Displays information about a user."; bCommand.Usage = "mb/userinfo <user>"; bCommand.Example = "mb/userinfo MarbleBot"; break;

                        // Economy
                        case "balance": bCommand.Desc = "Returns how much money you or someone else has."; bCommand.Usage = "mb/balance <optional user>"; break;
                        case "buy": bCommand.Desc = "Buys items."; bCommand.Usage = "mb/buy <item ID> <# of items>"; bCommand.Example = "mb/buy 1 1"; break;
                        case "daily": bCommand.Desc = "Gives daily Units of Money (200 to the power of your streak minus one). You can only do this every 24 hours."; bCommand.Usage = "mb/balance"; break;
                        case "item": bCommand.Desc = "Returns information about an item."; bCommand.Usage = "mb/item <item ID>"; break;
                        case "poupsoop": bCommand.Desc = "Calculates the total price of Poup Soop."; bCommand.Usage = "mb/poupsoop <# Regular> | <# Limited> | <# Frozen> | <# Orange> | <# Electric> | <# Burning> | <# Rotten> | <# Ulteymut> | <# Variety Pack>"; bCommand.Example = "mb/poupsoop 3 | 1"; break;
                        case "profile": bCommand.Desc = "Returns the profile of you or someone else."; bCommand.Usage = "mb/profile <optional user>"; break;
                        case "richlist": bCommand.Desc = "Shows the ten richest people globally by Net Worth."; bCommand.Usage = "mb/richlist"; break;
                        case "sell": bCommand.Desc = "Sells items."; bCommand.Usage = "mb/sell <item ID> <# of items>"; bCommand.Example = "mb/sell 1 1"; break;
                        case "shop": bCommand.Desc = "Shows all items available for sale, their IDs and their prices."; bCommand.Usage = "mb/shop"; break;

                        // Roles
                        case "give": bCommand.Desc = "Gives a role if it is on the role list."; bCommand.Usage = "mb/give <role>"; bCommand.Example = "mb/give Owner"; break;
                        case "role": bCommand.Desc = "Toggles role list roles."; bCommand.Usage = "mb/role <role>"; bCommand.Example = "mb/role Bots"; break;
                        case "rolelist": bCommand.Desc = "Shows a list of roles that can be given/taken by `mb/give` and `mb/take`."; bCommand.Usage = "mb/rolelist"; break;
                        case "take": bCommand.Desc = "Takes a role if it is on the role list."; bCommand.Usage = "mb/take <role>"; bCommand.Example = "mb/take Criminal"; break;

                        // YT
                        case "cv": bCommand.Desc = "Posts a video in #community-videos."; bCommand.Usage = "mb/cv <video link> <optional description>"; bCommand.Example = "A thrilling race made with an incredible, one of a kind feature! https://www.youtube.com/watch?v=7lp80lBO1Vs"; bCommand.Warning = "This command only works in DMs!"; break;
                        case "searchchannel": bCommand.Desc = "Displays a list of channels that match the search criteria."; bCommand.Usage = "mb/searchchannel <channelname>"; bCommand.Example = "mb/searchchannel carykh"; break;
                        case "searchvideo": bCommand.Desc = "Displays a list of videos that match the search critera."; bCommand.Usage = "mb/searchvideo <videoname>"; bCommand.Example = "mb/searchvideo The Amazing Marble Race"; break;

                        // Games
                        case "race": bCommand.Desc = "Participate in a marble race!"; bCommand.Usage = "mb/race signup <marble name>, mb/race contestants, mb/race remove <marble name>, mb/race start, mb/race leaderboards <winners/mostUsed>, mb/race checkearn"; break;
                        case "siege": bCommand.Desc = "Participate in a Marble Siege boss battle!"; bCommand.Usage = "mb/siege signup <marble name>, mb/siege contestants, mb/siege start, mb/siege attack, mb/siege grab, mb/siege info, mb/siege checkearn, mb/siege boss <boss name>, mb/siege bosslist, mb/siege powerup <power-up name>, mb/siege ping <on/off>"; break;
                    }

                    if (!bCommand.Desc.IsEmpty()) {
                        builder.WithDescription(bCommand.Desc)
                            .AddField("Usage", $"`{bCommand.Usage}`")
                            .WithTitle($"MarbleBot Help: **{bCommand.Name[0].ToString().ToUpper() + string.Concat(bCommand.Name.Skip(1))}**");

                        if (!bCommand.Example.IsEmpty()) builder.AddField("Example", $"`bCommand.Example`");
                        if (!bCommand.Warning.IsEmpty()) builder.AddField("Warning", $":warning: {bCommand.Warning}");

                        await ReplyAsync(embed: builder.Build());
                    } else await ReplyAsync("Could not find requested command!");
                    break;
            }
        }

        [Command("serverinfo")]
        [Summary("Displays information about a server.")]
        [Remarks("Not DMs")]
        public async Task ServerInfoCommandAsync()
        {
            await Context.Channel.TriggerTypingAsync();
            if (!Context.IsPrivate) {
                EmbedBuilder builder = new EmbedBuilder();
                int botUsers = 0;
                int onlineUsers = 0;
                SocketGuildUser[] users = Context.Guild.Users.ToArray();
                for (int i = 0; i < Context.Guild.Users.Count - 1; i++)
                {
                    if (users[i].IsBot) botUsers++;
                    if (users[i].Status.ToString().ToLower() == "online") onlineUsers++;
                }
                builder.WithThumbnailUrl(Context.Guild.IconUrl)
                    .WithTitle(Context.Guild.Name)
                    .AddField("Owner", Context.Guild.GetUser(Context.Guild.OwnerId).Username + "#" + Context.Guild.GetUser(Context.Guild.OwnerId).Discriminator, true)
                    .AddField("Voice Region", Context.Guild.VoiceRegionId, true)
                    .AddField("Text Channels", Context.Guild.TextChannels.Count, true)
                    .AddField("Voice Channels", Context.Guild.VoiceChannels.Count, true)
                    .AddField("Members", Context.Guild.Users.Count, true)
                    .AddField("Bots", botUsers, true)
                    .AddField("Online", onlineUsers)
                    .AddField("Roles", Context.Guild.Roles.Count, true)
                    .WithColor(Global.GetColor(Context))
                    .WithTimestamp(DateTime.UtcNow)
                    .WithFooter(Context.Guild.Id.ToString());
                await ReplyAsync(embed: builder.Build());
            } else await ReplyAsync("This is a DM, not a server!");
        }

        [Command("staffcheck")]
        [Summary("Displays a list of all staff members and their statuses.")]
        [Remarks("Not DMs")]
        public async Task StaffCheckCommandAsync()
        {
            await Context.Channel.TriggerTypingAsync();
            if (!Context.IsPrivate) {
                IGuildUser Doc671 = Context.Guild.GetUser(224267581370925056);
                if (Context.Guild.Id == Global.CM) {
                    IGuildUser Erikfassett = Context.Guild.GetUser(161258738429329408);
                    IGuildUser JohnDubuc = Context.Guild.GetUser(161247044713840642);
                    IGuildUser TAR = Context.Guild.GetUser(186652039126712320);
                    IGuildUser Algorox = Context.Guild.GetUser(323680030724980736);
                    IGuildUser FlameVapour = Context.Guild.GetUser(193247613095641090);
                    IGuildUser[] users = { Doc671, Erikfassett, JohnDubuc, TAR, Algorox, FlameVapour};
                    string[] nicks = new string[users.Length];
                    string[] statuses = new string[users.Length];
                    int h = 0;
                    foreach (var user in users) {
                        nicks[h] = user.Nickname;
                        statuses[h] = user.Status.ToString();
                        h++;
                    }
                    for (int i = 0; i < users.Length - 1; i++) {
                        if (nicks[i].IsEmpty()) nicks[i] = users[i].Username;
                        if (statuses[i] == "DoNotDisturb") statuses[i] = "Do Not Disturb";
                    }
                    var output = new StringBuilder();
                    output.Append("**__Admins:__** \n" + nicks[0] + " (" + users[0].Username + "#" + users[0].Discriminator + "): **" + statuses[0] + "**");
                    output.Append("**\n" + nicks[1] + " (" + users[1].Username + "#" + users[1].Discriminator + "): **" + statuses[1] + "**");
                    output.Append("**\n" + nicks[2] + " (" + users[2].Username + "#" + users[2].Discriminator + "): **" + statuses[2] + "**");
                    output.Append("**\n" + nicks[3] + " (" + users[3].Username + "#" + users[3].Discriminator + "): **" + statuses[3] + "**");
                    output.Append("\n\n**__Mods:__** \n" + nicks[4] + " (" + users[4].Username + "#" + users[4].Discriminator + "): **" + statuses[4] + "**");
                    output.Append("**\n" + nicks[5] + " (" + users[5].Username + "#" + users[5].Discriminator + "): **" + statuses[5] + "**");
                    await ReplyAsync(output.ToString());
                } else if (Context.Guild.Id == Global.THS) {
                    IGuildUser FlameVapour = Context.Guild.GetUser(193247613095641090);
                    IGuildUser DannyPlayz = Context.Guild.GetUser(329532528031563777);
                    IGuildUser George012 = Context.Guild.GetUser(232618363975630849);
                    IGuildUser Kenlimepie = Context.Guild.GetUser(195529549855850496);
                    IGuildUser[] users = { Doc671, FlameVapour, DannyPlayz, George012, Kenlimepie };
                    string[] nicks = new string[users.Length];
                    string[] statuses = new string[users.Length];
                    int h = 0;
                    foreach (var user in users) {
                        nicks[h] = user.Nickname;
                        statuses[h] = user.Status.ToString();
                        h++;
                    }
                    for (int i = 0; i < users.Length - 1; i++) {
                        if (nicks[i].IsEmpty()) nicks[i] = users[i].Username;
                        if (statuses[i] == "DoNotDisturb") statuses[i] = "Do Not Disturb";
                    }
                    var output = new StringBuilder();
                    output.Append($"**__Overlords:__** \n{nicks[0]} ({users[0].Username}#{users[0].Discriminator}): **{statuses[0]}**");
                    output.Append($"**\n{nicks[1]} ({users[1].Username}#{users[1].Discriminator}): **{statuses[1]}**");
                    output.Append($"\n\n**__Hat Stoar Employees:__** \n{nicks[2]} ({users[2].Username}#{users[2].Discriminator}): **{statuses[2]}**");
                    output.Append($"**\n{nicks[3]} ({users[3].Username}#{users[3].Discriminator}): **{statuses[3]}**");
                    output.Append($"**\n{nicks[4]} ({users[4].Username}#{users[4].Discriminator}): **{statuses[4]}**");
                    await ReplyAsync(output.ToString());
                } else if (Context.Guild.Id == Global.MT) {
                    IGuildUser George012 = Context.Guild.GetUser(232618363975630849);
                    IGuildUser[] users = { Doc671, George012 };
                    string[] nicks = { users[0].Nickname, users[1].Nickname, };
                    string[] statuses = { users[0].Status.ToString(), users[1].Status.ToString() };
                    for (int i = 0; i < users.Length; i++) {
                        if (nicks[i].IsEmpty()) nicks[i] = users[i].Username;
                        if (statuses[i] == "DoNotDisturb") statuses[i] = "Do Not Disturb";
                    }
                    await ReplyAsync(nicks[0] + " (" + users[0].Username + "#" + users[0].Discriminator + "): **" + statuses[0] + "**\n" + nicks[1] + " (" + users[1].Username + "#" + users[1].Discriminator + "): **" + statuses[1] + "**");
                } else if (Context.Guild.Id == Global.VFC) {
                    IGuildUser Vinh = Context.Guild.GetUser(311360247740760064);
                    IGuildUser George012 = Context.Guild.GetUser(232618363975630849);
                    IGuildUser Kenlimepie = Context.Guild.GetUser(195529549855850496);
                    IGuildUser Meadow = Context.Guild.GetUser(370463333763121152);
                    IGuildUser Ayumi = Context.Guild.GetUser(189713815414374404);
                    IGuildUser BlueIce57 = Context.Guild.GetUser(310960432909254667);
                    IGuildUser Miles = Context.Guild.GetUser(170804546438692864);
                    IGuildUser[] users = { Vinh, Kenlimepie, Doc671, George012, Meadow, Ayumi, BlueIce57, Miles };
                    string[] nicks = new string[users.Length];
                    string[] statuses = new string[users.Length];
                    int h = 0;
                    foreach (var user in users) {
                        nicks[h] = user.Nickname;
                        statuses[h] = user.Status.ToString();
                        h++;
                    }
                    for (int i = 0; i < users.Length - 1; i++) {
                        if (nicks[i].IsEmpty()) nicks[i] = users[i].Username;
                        if (statuses[i] == "DoNotDisturb") statuses[i] = "Do Not Disturb";
                    }
                    var output = new StringBuilder();
                    output.Append("**__Owner:__** \n" + nicks[0] + " (" + users[0].Username + "#" + users[0].Discriminator + "): **" + statuses[0]);
                    output.Append("**\n\n**__Co-owners:__** \n" + nicks[1] + " (" + users[1].Username + "#" + users[1].Discriminator + "): **" + statuses[1]);
                    output.Append("**\n" + nicks[2] + " (" + users[2].Username + "#" + users[2].Discriminator + "): **" + statuses[2]);
                    output.Append("\n\n**__Admins:__** \n" + nicks[3] + " (" + users[3].Username + "#" + users[3].Discriminator + "): **" + statuses[3]);
                    output.Append("**\n" + nicks[4] + " (" + users[4].Username + "#" + users[4].Discriminator + "): **" + statuses[4]);
                    output.Append("\n\n**__Mods:__** \n" + nicks[5] + " (" + users[5].Username + "#" + users[5].Discriminator + "): **" + statuses[5]);
                    output.Append("**\n" + nicks[6] + " (" + users[6].Username + "#" + users[6].Discriminator + "): **" + statuses[6]);
                    output.Append("**\n" + nicks[7] + " (" + users[7].Username + "#" + users[7].Discriminator + "): **" + statuses[7]);
                    await ReplyAsync(output.ToString());
                }
            } else await ReplyAsync("There are no staff members in a DM!");
        }

        [Command("uptime")]
        [Summary("Displays how long the bot has been running for.")]
        public async Task UptimeCommandAsync() {
            var timeDiff = DateTime.UtcNow.Subtract(Global.StartTime);
            await ReplyAsync("The bot has been running for **" + Global.GetDateString(timeDiff) + "**.");
        }

        [Command("userinfo")]
        [Summary("Displays information about a user.")]
        [Remarks("Not DMs")]
        public async Task UserInfoCommandAsync([Remainder] string username = "")
        {
            if (!Context.IsPrivate) {
                await Context.Channel.TriggerTypingAsync();
                EmbedBuilder builder = new EmbedBuilder();
                SocketGuildUser user = (SocketGuildUser)Context.User;
                var userFound = true;
                username = username.ToLower();
                if (username[0] == '<') {
                    try {
                        ulong.TryParse(username.Trim('<').Trim('>').Trim('@'), out ulong ID);
                        user = Context.Guild.GetUser(ID);
                    } catch (NullReferenceException ex) {
                        Trace.WriteLine($"[{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}] {ex}");
                        await ReplyAsync("Invalid ID!");
                        userFound = false;
                    }
                } else {
                    try {
                        user = Context.Guild.Users.Where(u => u.Username.ToLower().Contains(username) || username.Contains(u.Username.ToLower())).FirstOrDefault();
                    } catch (NullReferenceException ex) {
                        Trace.WriteLine($"[{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}] {ex}");
                        await ReplyAsync("Could not find the requested user!");
                        userFound = false;
                    }
                }
                if (userFound) { 
                    string status = "";
                    switch (user.Status) {
                        case UserStatus.Online: status = "Online"; break;
                        case UserStatus.Idle: status = "Idle"; break;
                        case UserStatus.DoNotDisturb: status = "Do Not Disturb"; break;
                        case UserStatus.AFK: status = "Idle"; break;
                        default: status = "Offline"; break;
                    }

                    string nickname;
                    if (user.Nickname.IsEmpty()) nickname = "None";
                    else nickname = user.Nickname;

                    var roles = new StringBuilder();
                    foreach (var role in user.Roles) {
                        roles.AppendLine(role.Name);
                    }

                    builder.WithAuthor(user)
                        .AddField("Status", status, true)
                        .AddField("Nickname", nickname, true)
                        .AddField("Registered", user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), true)
                        .AddField("Joined", ((DateTimeOffset)user.JoinedAt).ToString("yyyy-MM-dd HH:mm:ss"), true)
                        .AddField("Roles", roles.ToString(), true)
                        .WithColor(Global.GetColor(Context))
                        .WithCurrentTimestamp()
                        .WithFooter("All times in UTC, all dates YYYY-MM-DD.");

                    await ReplyAsync(embed: builder.Build());
                }
            }
        }
    }
}
