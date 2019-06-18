using Discord;
using Discord.Commands;
using MarbleBot.Core;
using MarbleBot.Extensions;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using static MarbleBot.Global;
using static MarbleBot.MarbleBotModule;

namespace MarbleBot.Modules
{
    /// <summary> Game commands. </summary>
    public partial class Games
    {
        private static string GameName(GameType gameType, bool capitalised = true)
        => capitalised ? Enum.GetName(typeof(GameType), gameType) : Enum.GetName(typeof(GameType), gameType).ToLower();

        public static async Task CheckearnAsync(SocketCommandContext context, GameType gameType)
        {
            await context.Channel.TriggerTypingAsync();
            var user = GetUser(context);
            var lastWin = gameType switch
            {
                GameType.Race => user.LastRaceWin,
                GameType.Siege => user.LastSiegeWin,
                GameType.War => user.LastWarWin,
                _ => user.LastScavenge,
            };
            var nextDaily = DateTime.UtcNow.Subtract(lastWin);
            var output = nextDaily.TotalHours < 6 ?
                $"You can earn money from racing in **{GetDateString(lastWin.Subtract(DateTime.UtcNow.AddHours(-6)))}**!"
                : "You can earn money from racing now!";
            await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithAuthor(context.User)
                .WithColor(GetColor(context))
                .WithCurrentTimestamp()
                .WithDescription(output)
                .Build());
        }

        public static async Task ClearAsync(SocketCommandContext context, GameType gameType)
        {
            await context.Channel.TriggerTypingAsync();
            ulong fileId = context.IsPrivate ? context.User.Id : context.Guild.Id;
            if (context.User.Id == 224267581370925056 || context.IsPrivate)
            {
                using var marbleList = new StreamWriter($"Data{Path.DirectorySeparatorChar}{fileId}{GameName(gameType, false)}.csv", false);
                await marbleList.WriteAsync("");
                await context.Channel.SendMessageAsync("Contestant list successfully cleared!");
            }
        }

        public static async Task ContestantsAsync(SocketCommandContext context, GameType gameType)
        {
            await context.Channel.TriggerTypingAsync();
            ulong fileId = context.IsPrivate ? context.User.Id : context.Guild.Id;
            var builder = new EmbedBuilder()
                .WithColor(GetColor(context))
                .WithCurrentTimestamp();
            var marbles = new StringBuilder();
            byte count = 0;
            using (var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}{GameName(gameType, false)}.csv"))
            {
                var allMarbles = (await marbleList.ReadToEndAsync()).Split('\n');
                foreach (var marble in allMarbles)
                {
                    if (marble.Length > 16)
                    {
                        var mSplit = marble.Split(',');
                        var user = context.Client.GetUser(ulong.Parse(mSplit[1].Trim('\n')));
                        if (context.IsPrivate) marbles.AppendLine($"**{mSplit[0].Trim('\n')}**");
                        else marbles.AppendLine($"**{mSplit[0].Trim('\n')}** [{user.Username}#{user.Discriminator}]");
                        count++;
                    }
                }
            }
            if (marbles.ToString().IsEmpty())
                await context.Channel.SendMessageAsync("It looks like there aren't any contestants...");
            else
            {
                builder.AddField("Contestants", marbles.ToString());
                builder.WithFooter("Contestant count: " + count)
                    .WithTitle($"Marble {GameName(gameType)}: Contestants");
                await context.Channel.SendMessageAsync(embed: builder.Build());
            }
        }

        public static async Task RemoveAsync(SocketCommandContext context, GameType gameType, string marbleToRemove)
        {
            ulong fileId = context.IsPrivate ? context.User.Id : context.Guild.Id;
            // 0 - Not found, 1 - Found but not yours, 2 - Found & yours, 3 - Found & overridden
            byte state = context.User.Id == 224267581370925056 ? (byte)3 : (byte)0;
            var wholeFile = new StringBuilder();
            using (var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}{GameName(gameType, false)}.csv"))
            {
                while (!marbleList.EndOfStream)
                {
                    var line = await marbleList.ReadLineAsync();
                    if (string.Compare(line.Split(',')[0], marbleToRemove, true) == 0)
                    {
                        if (ulong.Parse(line.Split(',')[1]) == context.User.Id)
                            state = 2;
                        else
                        {
                            wholeFile.AppendLine(line);
                            if (!(state == 2)) state = 1;
                        }
                    }
                    else wholeFile.AppendLine(line);
                }
            }
            switch (state)
            {
                case 0: await context.Channel.SendMessageAsync("Could not find the requested marble!"); break;
                case 1: await context.Channel.SendMessageAsync("This is not your marble!"); break;
                case 2:
                case 3:
                    using (var marbleList = new StreamWriter($"Data{Path.DirectorySeparatorChar}{fileId}{GameName(gameType, false)}.csv", false))
                    {
                        await marbleList.WriteAsync(wholeFile.ToString());
                        await context.Channel.SendMessageAsync($"Removed contestant **{marbleToRemove}**!");
                    }
                    break;
            }
        }

        public static async Task SignupAsync(SocketCommandContext context, GameType gameType, string marbleName, int marbleLimit,
            Func<Task> startCommand, string itemId = "")
        {
            await context.Channel.TriggerTypingAsync();
            ulong fileId = context.IsPrivate ? context.User.Id : context.Guild.Id;
            string marbleListDirectory = $"Data{Path.DirectorySeparatorChar}{fileId}{GameName(gameType, false)}.csv";

            if (gameType == GameType.Siege || gameType == GameType.War)
            {
                if (gameType == GameType.Siege)
                {
                    if (SiegeInfo.ContainsKey(fileId))
                    {
                        await context.Channel.SendMessageAsync($"**{context.User.Username}**, a battle is currently ongoing!");
                        return;
                    }
                }
                else if (gameType == GameType.War)
                {
                    var item = GetItem(itemId);
                    if (item.WarClass == 0)
                    {
                        await context.Channel.SendMessageAsync($"**{context.User.Username}**, this item cannot be used as a weapon!");
                        return;
                    }
                    var user = GetUser(context);
                    if (!user.Items.ContainsKey(item.Id) || user.Items[item.Id] < 1)
                    {
                        await context.Channel.SendMessageAsync($"**{context.User.Username}**, you don't have this item!");
                        return;
                    }
                    if (SiegeInfo.ContainsKey(fileId))
                    {
                        await context.Channel.SendMessageAsync($"**{context.User.Username}**, a battle is currently ongoing!");
                        return;
                    }
                }
                using var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}{GameName(gameType, false)}.csv");
                if ((await marbleList.ReadToEndAsync()).Contains(context.User.Id.ToString()))
                {
                    await context.Channel.SendMessageAsync($"**{context.User.Username}**, you've already joined!");
                    return;
                }
            }

            if (marbleName.IsEmpty() || marbleName.Contains("@")) marbleName = context.User.Username;
            else if (marbleName.Length > 100)
            {
                await context.Channel.SendMessageAsync($"**{context.User.Username}**, your entry exceeds the 100 character limit.");
                return;
            }
            else marbleName = marbleName.Replace('\n', ' ').Replace(',', ';');

            var builder = new EmbedBuilder()
                .WithColor(GetColor(context))
                .WithCurrentTimestamp()
                .AddField($"Marble {GameName(gameType)}: Signed up!", $"**{context.User.Username}** has successfully signed up as **{marbleName}**!");
            using (var racers = new StreamWriter($"Data{Path.DirectorySeparatorChar}{GameName(gameType)}MostUsed.txt", true))
                await racers.WriteLineAsync(marbleName);
            if (!File.Exists(marbleListDirectory)) File.Create(marbleListDirectory).Close();
            using (var marbleList = new StreamWriter(marbleListDirectory, true))
            {
                if (gameType == GameType.War) await marbleList.WriteLineAsync($"{marbleName},{context.User.Id},{itemId}");
                else await marbleList.WriteLineAsync($"{marbleName},{context.User.Id}");
            }

            int marbleNo;
            using (var marbleList = new StreamReader(marbleListDirectory, true))
                marbleNo = (await marbleList.ReadToEndAsync()).Split('\n').Length;
            await context.Channel.SendMessageAsync(embed: builder.Build());
            if (marbleNo > marbleLimit - 1)
            {
                await context.Channel.SendMessageAsync($"The limit of {marbleLimit} contestants has been reached!");
                await startCommand();
            }
        }
    }
}
