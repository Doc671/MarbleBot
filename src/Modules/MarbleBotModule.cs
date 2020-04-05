using Discord;
using Discord.Commands;
using MarbleBot.Common;
using NLog;
using System;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Modules
{
    public abstract class MarbleBotModule : ModuleBase<SocketCommandContext>
    {
        // Server IDs
        protected internal const ulong CommunityMarble = 223616088263491595;
        protected internal const ulong TheHatStoar = 224277738608001024;

        protected internal const string UnitOfMoney = "<:unitofmoney:372385317581488128>";

        protected Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        protected internal static Color GetColor(SocketCommandContext context)
        {
            if (context.IsPrivate)
            {
                return Color.DarkerGrey;
            }
            else
            {
                return new Color(uint.Parse(MarbleBotGuild.Find(context).Color, System.Globalization.NumberStyles.HexNumber));
            }
        }

        protected internal static string GetDateString(TimeSpan dateTime)
        {
            var output = new StringBuilder();
            if (dateTime.Days > 1)
            {
                output.Append($"{dateTime.Days} days, ");
            }
            else if (dateTime.Days == 1)
            {
                output.Append("1 day, ");
            }

            if (dateTime.Hours > 1)
            {
                output.Append($"{dateTime.Hours} hours, ");
            }
            else if (dateTime.Hours == 1)
            {
                output.Append("1 hour, ");
            }

            if (dateTime.Minutes > 1)
            {
                output.Append($"{dateTime.Minutes} minutes ");
            }
            else if (dateTime.Minutes == 1)
            {
                output.Append("1 minute ");
            }

            if (dateTime.Seconds > 1)
            {
                if (dateTime.Minutes > 0)
                {
                    output.Append($"and {dateTime.Seconds} seconds");
                }
                else
                {
                    output.Append(dateTime.Seconds + " seconds");
                }
            }
            else if (dateTime.Seconds == 1)
            {
                if (dateTime.Minutes > 0)
                {
                    output.Append("and 1 second");
                }
                else
                {
                    output.Append("1 second");
                }
            }
            else if (dateTime.TotalSeconds < 1)
            {
                if (dateTime.Minutes > 0)
                {
                    output.Append("and <1 second");
                }
                else
                {
                    output.Append("<1 second");
                }
            }
            return output.ToString();
        }

        protected internal static string GetDateString(double seconds)
        {
            if (seconds == 1)
            {
                return "**1** second";
            }
            else
            {
                return $"**{seconds:n1}** seconds";
            }
        }

        protected internal async Task<IUserMessage> SendErrorAsync(string messageContent)
            => await ReplyAsync($":warning: | {messageContent}");

        protected static string StageTooHighString()
            => (new Random().Next(0, 6)) switch
            {
                0 => "*Your inexperience blinds you...*",
                1 => "*Your vision is blurry...*",
                2 => "*Incomprehensible noises rattle in your head...*",
                3 => "*You sense a desk restricting your path...*",
                4 => "*You feel as if there is more to be done...*",
                _ => "*Your mind is wracked with pain...*",
            };
    }
}
