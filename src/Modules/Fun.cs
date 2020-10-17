using Discord;
using Discord.Commands;
using MarbleBot.Common;
using MarbleBot.Extensions;
using MarbleBot.Services;
using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = System.Drawing.Color;

namespace MarbleBot.Modules
{
    [Summary("Fun, non-game commands.")]
    public class Fun : MarbleBotModule
    {
        private readonly BotCredentials _botCredentials;
        private readonly RandomService _randomService;

        public Fun(BotCredentials botCredentials, RandomService randomService)
        {
            _botCredentials = botCredentials;
            _randomService = randomService;
        }

        [Command("7ball")]
        [Summary("Predicts the outcome to a user-defined event.")]
        [RequireMaximumLength(100)]
        public async Task SevenBallCommand([Remainder] string _)
        {
            string outcome = _randomService.Rand.Next(0, 13) switch
            {
                0 => "no.",
                1 => "looking negative.",
                2 => "probably not.",
                3 => "it is very doubtful.",
                4 => "my visions are cloudy, try again another time.",
                5 => "do you *really* want to know?",
                6 => "I forgot.",
                7 => "possibly.",
                8 => "it is highly likely.",
                9 => "I believe so.",
                10 => "it is certain.",
                11 => "and the sign points to... yes!",
                12 => "and the sign points to... no!",
                _ => "probably not, but there is still a chance..."
            };
            await ReplyAsync($":seven: |  **{Context.User.Username}**, {outcome}");
        }

        [Command("advice")]
        [Alias("progress", "sage")]
        [Summary("Gives advice on progression.")]
        public async Task AdviceCommand()
        {
            MarbleBotUser user = MarbleBotUser.Find(Context);
            string msg;
            if (user.Items.ContainsKey(78))
            {
                msg = new StringBuilder().Append("Combine the terror-inducing essence of the bosses you have just ")
                    .AppendLine("defeated with another similar, yet different liquid. Be careful with the product.")
                    .Append("\n**TO ADVANCE:** Craft the **Essence of Corruption** (ID `079`).")
                    .ToString();
            }
            else if (user.Items.ContainsKey(66) || user.Items.ContainsKey(71) || user.Items.ContainsKey(74) ||
                     user.Items.ContainsKey(80))
            {
                msg = new StringBuilder().Append("Your equipment will prove very useful in the upcoming battles.")
                    .AppendLine("Seek the Chest of sentience and the Scary Face to test your newfound power.")
                    .Append("\n**TO ADVANCE:** Obtain the **Raw Essence of Horror** (ID `078`) from Chest or Scary Face.")
                    .ToString();
            }
            else if (user.Items.ContainsKey(81))
            {
                msg = new StringBuilder().Append("There is a way to increase the offensive capabilities of a marble.")
                    .AppendLine("Form a covering of spikes, made of iron, steel or even infernite.")
                    .Append("\n**TO ADVANCE:** Craft **Iron Spikes** (ID `066`), **Steel Spikes** (ID `071`) or **Infernite Spikes** (ID `074`).")
                    .ToString();
            }
            else if (user.Items.ContainsKey(63))
            {
                msg = new StringBuilder().Append("The world now contains a plethora of treasures for you to gather.")
                    .AppendLine("Craft the drill of chromium to allow you to extract the ore of the Violet Volcanoes.")
                    .Append("\n**TO ADVANCE:** Craft the **Chromium Drill** (ID `081`).")
                    .ToString();
            }
            else if (user.Items.ContainsKey(62))
            {
                msg = new StringBuilder()
                    .Append("Before you can successfully take on the new terrors roaming the land, ")
                    .AppendLine("you must first improve your equipment. Use Destroyer's plating to craft your own shield.")
                    .Append("\n**TO ADVANCE:** Craft the **Coating of Destruction** (ID `063`).")
                    .ToString();
            }
            else if (user.Stage == 2)
            {
                msg = new StringBuilder()
                    .Append("The cyborg's defeat has both given you new options and caught the attention of ")
                    .AppendLine("even more powerful foes. Head to its remains and gather the resources to upgrade your workstation.")
                    .Append("\n**TO ADVANCE:** Craft the **Crafting Station Mk.II** (ID `062`).")
                    .ToString();
            }
            else if (user.Items.ContainsKey(53) && user.Items.ContainsKey(57))
            {
                msg = new StringBuilder()
                    .Append("You have done very well, and have forged the best with the resources available ")
                    .AppendLine("to you. There is more to this world, however. Gather your allies and seek the cyborg Destroyer.")
                    .Append("\n**TO ADVANCE:** Defeat Destroyer. Item `091` may provide assistance.")
                    .ToString();
            }
            else if (user.Items.ContainsKey(53))
            {
                msg = new StringBuilder()
                    .Append("The Trebuchet Array is a potent weapon, albeit rather inaccurate. To assist ")
                    .AppendLine("in your battles, create the Rocket Boots, which shall help you evade their menacing attacks.")
                    .Append("\n**TO ADVANCE:** Craft the **Rocket Boots** (ID `057`).")
                    .ToString();
            }
            else if (user.Items.ContainsKey(17))
            {
                msg = new StringBuilder()
                    .Append("With your workstation, forge the Trebuchet Array from the different woods found ")
                    .AppendLine("in the forest. You will have to create three separate trebuchets first, then combine them.")
                    .Append("\n**TO ADVANCE:** Craft the **Trebuchet Array** (ID `053`).")
                    .ToString();
            }
            else if (user.LastScavenge.DayOfYear != 1 || user.LastScavenge.Year != 2019)
            {
                msg = new StringBuilder()
                    .Append("The items you have gathered are likely unable to be used in their current form. ")
                    .AppendLine("You must find a way to obtain a Crafting Station.")
                    .Append("\n**TO ADVANCE:** Obtain the **Crafting Station Mk.I** (ID `017`) via dailies.")
                    .ToString();
            }
            else if (user.NetWorth > 1000)
            {
                msg = new StringBuilder()
                    .Append("Well done. Your next goal is to gather for items at Canary Beach and Tree Wurld. ")
                    .AppendLine("Use `mb/scavenge help` if you are unsure of how to proceed.")
                    .Append("\n**TO ADVANCE:** Successfully complete a Scavenge.")
                    .ToString();
            }
            else
            {
                msg = new StringBuilder()
                    .Append($"Welcome! Your first task is to gain {UnitOfMoney}1000! If you need help ")
                    .AppendLine("earning money, try using `mb/daily`, `mb/race` or `mb/siege`.")
                    .Append($"\n**TO ADVANCE:** Obtain {UnitOfMoney}1000.")
                    .ToString();
            }

            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithDescription(msg)
                .WithTitle($"Advice: {Context.User.Username}")
                .Build());
        }

        [Command("best")]
        [Summary("Picks a random person to call the best.")]
        [RequireContext(ContextType.Guild)]
        public async Task BestCommand()
        {
            await ReplyAsync($"**{Context.Guild.Users.ElementAt(_randomService.Rand.Next(0, Context.Guild.Users.Count))}** is the best!");
        }

        [Command("cameltotitlecase")]
        [Alias("cameltotitle")]
        [Summary("Converts a camel case string to title case.")]
        [RequireMaximumLength(100)]
        public async Task CamelToTitleCaseCommand([Remainder] string input)
        {
            await ReplyAsync(input.CamelToTitleCase());
        }

        [Command("choose")]
        [Summary("Chooses between several provided choices.")]
        [RequireMaximumLength(200)]
        public async Task ChooseCommand([Remainder] string input)
        {
            string[] choices = input.Split('|');
            int choice = _randomService.Rand.Next(0, choices.Length);
            await ReplyAsync($"**{Context.User.Username}**, I choose **{choices[choice].Trim()}**!");
        }

        [Command("color")]
        [Alias("colour")]
        [Summary("Shows info about a colo(u)r using its RGB values.")]
        public async Task ColorCommand(int red, int green, int blue)
        {
            if (red > byte.MaxValue || red < byte.MinValue
                                    || green > byte.MaxValue || green < byte.MinValue
                                    || blue > byte.MaxValue || blue < byte.MinValue)
            {
                await SendErrorAsync("The red, green and blue values must be integers between 0 and 255.");
                return;
            }

            Color color = Color.FromArgb(red, green, blue);
            color.GetHsv(out float hue, out float saturation, out float value);
            var builder = new EmbedBuilder()
                .AddField("RGB", $"Red: **{color.R}**\nGreen: **{color.G}**\nBlue: **{color.B}**", true)
                .AddField("HSV", $"Hue: **{hue}**\nSaturation: **{saturation}**\nValue: **{value}**", true)
                .AddField("HSL",
                    $"Hue: **{color.GetHue()}**\nSaturation: **{color.GetSaturation()}**\nLightness: **{color.GetBrightness()}**",
                    true)
                .AddField("Hex Code", $"#{color.R:X2}{color.G:X2}{color.B:X2}");

            await SendColorMessage(color, builder);
        }

        [Command("color")]
        [Alias("colour")]
        [Summary("Shows info about a colo(u)r using its HSV values.")]
        public async Task ColorCommand(float hue, float saturation, float value)
        {
            if (hue < 0f || hue > 360f)
            {
                await SendErrorAsync("The given hue must be between 0 and 360!");
                return;
            }

            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            int v = Convert.ToInt32(value * 255);
            int p = Convert.ToInt32(v * (1 - saturation));
            int q = Convert.ToInt32(v * (1 - f * saturation));
            int t = Convert.ToInt32(v * (1 - (1 - f) * saturation));
            Color color = hi switch
            {
                0 => Color.FromArgb(v, t, p),
                1 => Color.FromArgb(q, v, p),
                2 => Color.FromArgb(p, v, t),
                3 => Color.FromArgb(p, q, v),
                4 => Color.FromArgb(t, p, v),
                _ => Color.FromArgb(v, p, q)
            };
            var builder = new EmbedBuilder()
                .AddField("RGB", $"Red: **{color.R}**\nGreen: **{color.G}**\nBlue: **{color.B}**", true)
                .AddField("HSV", $"Hue: **{hue}**\nSaturation: **{saturation}**\nValue: **{value}**", true)
                .AddField("HSL", $"Hue: **{hue}**\nSaturation: **{color.GetSaturation()}**\nLightness: **{color.GetBrightness()}**", true)
                .AddField("Hex Code", $"#{color.R:X2}{color.G:X2}{color.B:X2}");

            await SendColorMessage(color, builder);
        }

        [Command("color")]
        [Alias("colour")]
        [Summary("Shows info about a colo(u)r using its hex code.")]
        public async Task ColorCommand(string hexCode)
        {
            hexCode = hexCode.RemoveChar('#');

            if (hexCode.Length != 6)
            {
                await SendErrorAsync("Invalid hex code. Please enter a six-digit hexadecimal RGB code.");
                return;
            }

            if (!int.TryParse(hexCode[..2], NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out int red)
                || !int.TryParse(hexCode[2..4], NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out int green)
                || !int.TryParse(hexCode[4..6], NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out int blue))
            {
                await SendErrorAsync("Could not parse the given hex code!");
                return;
            }

            Color color = Color.FromArgb(red, green, blue);
            color.GetHsv(out float hue, out float saturation, out float value);

            var builder = new EmbedBuilder()
                .AddField("RGB", $"Red: **{color.R}**\nGreen: **{color.G}**\nBlue: **{color.B}**", true)
                .AddField("HSV", $"Hue: **{hue}**\nSaturation: **{saturation}**\nValue: **{value}**", true)
                .AddField("HSL", $"Hue: **{color.GetHue()}**\nSaturation: **{color.GetSaturation()}**\nLightness: **{color.GetBrightness()}**", true)
                .AddField("Hex Code", $"#{hexCode.ToUpper()}");

            await SendColorMessage(color, builder);
        }

        private async Task SendColorMessage(Color color, EmbedBuilder builder)
        {
            builder.WithColor(new Discord.Color(color.R, color.G, color.B));

            var allColors = Enum.GetValues(typeof(KnownColor))
                .Cast<KnownColor>()
                .Where(c => Color.FromKnownColor(c).ToArgb() == color.ToArgb())
                .ToArray();

            if (allColors.Length != 0)
            {
                builder.WithTitle(allColors[0].ToString().CamelToTitleCase());
            }

            await ReplyAsync(embed: builder.Build());
        }

        [Command("random")]
        [Summary("Returns a random number with user-defined bounds.")]
        public async Task RandomCommand(int start, int end)
        {
            if (start > end)
            {
                await ReplyAsync(_randomService.Rand.Next(end, start).ToString());
            }
            else
            {
                await ReplyAsync(_randomService.Rand.Next(start, end).ToString());
            }
        }

        [Command("rate")]
        [Summary("Rates something out of 10.")]
        public async Task RateCommand([Remainder] string input)
        {
            int rating = _randomService.Rand.Next(0, 11);

            string emoji = rating switch
            {
                0 => ":no_entry_sign:",
                1 => ":nauseated_face:",
                2 => ":rage:",
                3 => ":angry:",
                4 => ":slight_frown:",
                5 => ":neutral_face:",
                6 => ":slight_smile:",
                7 => ":grinning:",
                8 => ":thumbsup:",
                9 => ":white_check_mark:",
                10 => ":rofl:",
                _ => ":thinking:"
            };

            string message = rating switch
            {
                0 => "Excuse me, kind user, please cease your current course of action immediately.",
                1 => "Immediate desistance required.",
                2 => "I don't like it...",
                3 => ">:(",
                4 => "ehhh...",
                5 => "not bad... but not good either",
                6 => "slightly above average... I guess...",
                7 => "pretty cool, don't you think?",
                8 => "yes",
                9 => "approaching perfection",
                10 => "PERFECT!!",
                _ => "Uhhhhhhhh\nNot"
            };

            await ReplyAsync($"**{Context.User.Username}**, I rate {input} **{rating}**/10. {emoji}\n({message})");
        }

        [Command("repeat")]
        [Summary("Repeats the given message.")]
        [RequireMaximumLength(100)]
        public async Task RepeatCommand([Remainder] string input)
        {
            await ReplyAsync(input);
        }

        [Command("reverse")]
        [Alias("orangeify")]
        [Summary("Returns the user input reversed.")]
        [RequireMaximumLength(100)]
        public async Task ReverseCommand([Remainder] string input)
        {
            await ReplyAsync(string.Concat(input.Reverse()));
        }

        [Command("submit")]
        [Summary("Submits a file to the bot owner.")]
        public async Task SubmitCommand([Remainder] string message = "")
        {
            if (string.IsNullOrEmpty(message) && Context.Message.Attachments.Count == 0)
            {
                await SendErrorAsync("You must submit a file!");
                return;
            }

            foreach (ulong adminId in _botCredentials.AdminIds)
            {
                if (Context.Message.Attachments.Count == 0)
                {
                    await Context.Client.GetUser(adminId).SendMessageAsync($"{Context.User.Id} {message}");
                }
                else
                {
                    await Context.Client.GetUser(adminId)
                        .SendMessageAsync($"{Context.User.Id} {message} {Context.Message.Attachments.First().Url}");
                }
            }

            await ReplyAsync("Your submission has been received.");
        }
    }
}
