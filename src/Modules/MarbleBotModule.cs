using Discord;
using Discord.Commands;
using MarbleBot.Common;
using NLog;
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Modules
{
    public abstract class MarbleBotModule : ModuleBase<SocketCommandContext>
    {
        // Server IDs
        protected const ulong CommunityMarble = 223616088263491595;

        protected internal const string UnitOfMoney = "<:unitofmoney:372385317581488128>";

        protected Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        protected internal static Color GetColor(SocketCommandContext context)
        {
            return context.IsPrivate
                ? Color.DarkerGrey
                : new Color(uint.Parse(MarbleBotGuild.Find(context).Color, NumberStyles.HexNumber));
        }

        protected static string GetTimeSpanSentence(TimeSpan timeSpan)
        {
            var output = new StringBuilder();
            int[] times = { timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds };
            string[] timeStrings = { "day", "hour", "minute", "second" };
            int penultimatePartIndex = 0;
            int lastPartIndex = 0;
            for (int i = 0; i < times.Length; i++)
            {
                if (times[i] != 0)
                {
                    penultimatePartIndex = lastPartIndex;
                    lastPartIndex = output.Length;
                    output.Append($"**{times[i]}** {timeStrings[i]}{(times[i] == 1 ? "" : "s")}, ");
                }
                else if (output.Length == 0 && i == 3)
                {
                    // if nothing else has been displayed and <1 second is left, display "<1 second"
                    penultimatePartIndex = lastPartIndex;
                    lastPartIndex = output.Length;
                    output.Append("**<1** second, ");
                }
            }

            output.Remove(output.Length - 2, 2); // remove final ", "
            if (lastPartIndex != 0)
            {
                output.Replace(", ", " and ", penultimatePartIndex, output.Length - penultimatePartIndex);
            }

            return output.ToString();
        }

        protected internal static string GetDateString(double seconds)
        {
            return seconds - 1 < double.Epsilon ? "**1** second" : $"**{seconds:n1}** seconds";
        }

        protected async Task<IUserMessage> SendErrorAsync(string messageContent)
        {
            return await ReplyAsync($":warning: | {messageContent}");
        }

        protected async Task<IUserMessage> SendSuccessAsync(string messageContent)
        {
            return await ReplyAsync($":white_check_mark: | {messageContent}");
        }

        protected async Task SendLargeEmbedDescriptionAsync(EmbedBuilder builder, string content)
        {
            if (content.Length > EmbedBuilder.MaxDescriptionLength)
            {
                bool endOfMessageReached = false;
                int currentMessageNo = 0;
                while (!endOfMessageReached)
                {
                    string currentMessageSlice = content[(EmbedBuilder.MaxDescriptionLength * currentMessageNo)..(EmbedBuilder.MaxDescriptionLength * (currentMessageNo + 1))];

                    if (currentMessageSlice.Length < EmbedBuilder.MaxDescriptionLength)
                    {
                        endOfMessageReached = true;
                    }
                    else
                    {
                        currentMessageNo++;
                    }

                    builder.WithDescription(currentMessageSlice);
                    await ReplyAsync(embed: builder.Build());
                }
            }
            else
            {
                builder.WithDescription(content);
                await ReplyAsync(embed: builder.Build());
            }
        }

        protected static string StageTooHighString()
        {
            return new Random().Next(0, 6) switch
            {
                0 => "*Your inexperience blinds you...*",
                1 => "*Your vision is blurry...*",
                2 => "*Incomprehensible noises rattle in your head...*",
                3 => "*You sense a desk restricting your path...*",
                4 => "*You feel as if there is more to be done...*",
                _ => "*Your mind is wracked with pain...*"
            };
        }
    }
}
