using Discord;
using Discord.Commands;
using MarbleBot.Core;
using MarbleBot.Extensions;
using System;
using System.IO;
using System.Threading.Tasks;

using static MarbleBot.Global;

namespace MarbleBot.Modules
{
    public partial class Games
    {
        private static string GameName(GameType gameType, bool capitalised = true)
        => capitalised ? Enum.GetName(typeof(GameType), gameType) : Enum.GetName(typeof(GameType), gameType).ToLower();

        public static async Task Signup(SocketCommandContext context, GameType gameType, string marbleName, int marbleLimit,
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
                    var item = MarbleBotModule.GetItem(itemId);
                    if (item.WarClass == 0)
                    {
                        await context.Channel.SendMessageAsync($"**{context.User.Username}**, this item cannot be used as a weapon!");
                        return;
                    }
                    var user = MarbleBotModule.GetUser(context);
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
                .WithColor(MarbleBotModule.GetColor(context))
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
