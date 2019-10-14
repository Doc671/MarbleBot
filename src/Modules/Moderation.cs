using Discord;
using Discord.Commands;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using MarbleBot.Extensions;
using MarbleBot.Services;
using NLog;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MarbleBot.Modules
{
    /// <summary> Moderation commands. </summary>
    public class Moderation : MarbleBotModule
    {
        private readonly BotCredentials _botCredentials;

        public Moderation(BotCredentials botCredentials)
        {
            _botCredentials = botCredentials;
        }

        [Command("addrole")]
        [Summary("Adds a role to the role list.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task AddRoleCommand([Remainder] string searchTerm)
        {
            if (!Context.Guild.Roles.Any(r => string.Compare(r.Name, searchTerm, true) == 0))
            {
                await SendErrorAsync("Could not find the role!");
                return;
            }
            var id = Context.Guild.Roles.Where(r => string.Compare(r.Name, searchTerm, true) == 0).First().Id;
            var guild = GetGuild(Context);
            guild.Roles.Add(id);
            WriteGuilds(GetGuildsObject(), Context.Guild, guild);
            await ReplyAsync("Succesfully updated.");
        }

        [Command("addwarningsheet")]
        [Alias("addwsheet", "addsheet", "setsheet", "setwsheet", "setwarningsheet")]
        [Summary("Sets the server's warning sheet link.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task AddWarningSheetCommand(string link)
        {
            var guild = GetGuild(Context);
            guild.WarningSheetLink = link;
            WriteGuilds(GetGuildsObject(), Context.Guild, guild);
            await ReplyAsync("Success.");
        }

        [Command("clear")]
        [Summary("Deletes the specified amount of messages.")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task ClearCommand(uint amount)
        {
            var messages = await Context.Channel.GetMessagesAsync((int)amount + 1).FlattenAsync();
            foreach (var msg in messages) await Context.Channel.DeleteMessageAsync(msg);
            const int delay = 5000;
            var m = await ReplyAsync($"{amount} message(s) have been deleted. This message will be deleted in {delay / 1000} seconds.");
            await Task.Delay(delay);
            await m.DeleteAsync();
        }

        [Command("clearchannel")]
        [Summary("Clears channels.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task ClearChannelCommand(string option)
        {
            var guild = GetGuild(Context);
            switch (option.ToLower().RemoveChar(' '))
            {
                case "announcement": guild.AnnouncementChannel = 0; break;
                case "autoresponse": guild.AutoresponseChannel = 0; break;
                case "usable": guild.UsableChannels = new List<ulong>(); break;
                default: await ReplyAsync("Invalid option. Use `mb/help clearchannel` for more info."); return;
            }
            WriteGuilds(GetGuildsObject(), Context.Guild, guild);
            await ReplyAsync("Succesfully cleared.");
        }

        [Command("clearrecentspam")]
        [Alias("clear-recent-spam")]
        [Summary("Clears recent empty messages")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task ClearRecentSpamCommand(string rguild, string rchannel)
        {
            var guild = ulong.Parse(rguild);
            var channel = ulong.Parse(rchannel);
            var msgs = await Context.Client.GetGuild(guild).GetTextChannel(channel).GetMessagesAsync(100).FlattenAsync();
            var srvr = new EmbedAuthorBuilder();
            if (guild == THS)
            {
                var _THS = Context.Client.GetGuild(THS);
                srvr.WithName(_THS.Name);
                srvr.WithIconUrl(_THS.IconUrl);
            }
            else if (guild == CM)
            {
                var _CM = Context.Client.GetGuild(CM);
                srvr.WithName(_CM.Name);
                srvr.WithIconUrl(_CM.IconUrl);
            }
            var builder = new EmbedBuilder()
                .WithAuthor(srvr)
                .WithTitle("Bulk delete in " + Context.Client.GetGuild(guild).GetTextChannel(channel).Mention)
                .WithColor(Discord.Color.Red)
                .WithDescription("Reason: Empty Messages")
                .WithFooter("ID: " + channel)
                .WithCurrentTimestamp();
            foreach (var msg in msgs)
            {
                var IsLett = !(char.TryParse(msg.Content.Trim('`'), out char e));
                if (!IsLett) IsLett = (char.IsLetter(e) || e == '?' || e == '^' || char.IsNumber(e));
                if (!(IsLett) && channel != 252481530130202624)
                {
                    builder.AddField(msg.Author.Mention, msg.Content);
                    await msg.DeleteAsync();
                }
            }
            if (guild == THS)
            {
                var logs = Context.Client.GetGuild(THS).GetTextChannel(327132239257272327);
                await logs.SendMessageAsync("", false, builder.Build());
            }
            else if (guild == CM)
            {
                var logs = Context.Client.GetGuild(CM).GetTextChannel(387306347936350213);
                await logs.SendMessageAsync("", false, builder.Build());
            }
        }


        // Checks if a string contains a swear; returns true if profanity is present
        public static async Task<bool> CheckSwearAsync(string msg)
        {
            string swears;
            using (var FS = new StreamReader($"Keys{Path.DirectorySeparatorChar}ListOfBand.txt")) swears = await FS.ReadLineAsync();
            string[] swearList = swears.Split(',');
            var swearPresent = false;
            foreach (var swear in swearList)
            {
                if (msg.ToLower().Contains(swear))
                {
                    swearPresent = true;
                    break;
                }
            }
            if (swearPresent) LogManager.GetCurrentClassLogger().Warn($"Profanity detected, violation: {msg}");
            return swearPresent;
        }

        public async Task LogWarningAsync(IGuildUser user, string warningCode, int warningsToGive)
        {
            using var service = new SheetsService(new BaseClientService.Initializer()
            {
                ApiKey = _botCredentials.GoogleApiKey,
                ApplicationName = GetType().ToString()
            });

            string spreadsheetId = GetGuild(Context).WarningSheetLink;
            const string range = "Warnings!A3:J";

            var result = await service.Spreadsheets.Values.Get(spreadsheetId, range).ExecuteAsync();

            // Get the warning sheet
            int? sheetId = (await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync()).Sheets.ToList()
                .Find(sheet => sheet.Properties.Title == "Warnings").Properties.SheetId;

            if (sheetId == null)
            {
                await SendErrorAsync("Could not find the warning sheet!");
                return;
            }

            var userToWarnRow = new string[10];

            int rowIndex = 3;

            // Find the row corresponding to the user to be warned
            for (rowIndex = 3; rowIndex < result.Values.Count + 3; rowIndex++)
            {
                var row = result.Values[rowIndex - 3];
                if ((row.First() as string).Contains(user.ToString()))
                {
                    userToWarnRow = row.Select(cell => cell.ToString()).ToArray();
                    break;
                }
            }

            var requests = new List<Request>();

            // If the user could not be found, add a new row with the user's details
            if (userToWarnRow[0] == null)
            {
                userToWarnRow = new string[] { user.ToString(), "", "", "", "", "", "", "Normal", "0", "0" };
                var rowData = new RowData()
                {
                    Values = new List<CellData>()
                };

                int cellNumber = 0;
                ExtendedValue cellContents;
                foreach (var cell in userToWarnRow)
                {
                    cellContents = new ExtendedValue();

                    // If the cell is a number, write it as a number rather than a string
                    if (int.TryParse(cell, out cellNumber))
                        cellContents.NumberValue = cellNumber;
                    else
                        cellContents.StringValue = cell;

                    rowData.Values.Add(new CellData()
                    {
                        UserEnteredValue = cellContents
                    });
                }

                requests.Add(new Request()
                {
                    AppendCells = new AppendCellsRequest()
                    {
                        SheetId = sheetId,
                        Rows = new List<RowData>() {
                            rowData
                        },
                        Fields = "*"
                    }
                });
            }

            int expiredWarnings = int.Parse(userToWarnRow[8]);
            int totalWarnings = int.Parse(userToWarnRow[9]);

            int currentWarnings = totalWarnings - expiredWarnings;

            totalWarnings += warningsToGive;

            static Google.Apis.Sheets.v4.Data.Color GetCellBackgroundColour(int warnings)
                => warnings switch
                {
                    3 => new Google.Apis.Sheets.v4.Data.Color
                    {
                        Red = 1f,
                        Green = 0.5f,
                        Blue = 0f,
                    },
                    4 => new Google.Apis.Sheets.v4.Data.Color
                    {
                        Red = 1f,
                        Green = 0f,
                        Blue = 0f,
                    },
                    5 => new Google.Apis.Sheets.v4.Data.Color
                    {
                        Red = 0.5f,
                        Green = 0f,
                        Blue = 0f,
                    },
                    6 => new Google.Apis.Sheets.v4.Data.Color
                    {
                        Red = 0f,
                        Green = 0f,
                        Blue = 0f,
                    },
                    _ => new Google.Apis.Sheets.v4.Data.Color
                    {
                        Red = 1f,
                        Green = 1f,
                        Blue = 0f,
                    }
                };

            static Google.Apis.Sheets.v4.Data.Color GetCellForegroundColour(int warnings)
                => warnings switch
                {
                    5 => new Google.Apis.Sheets.v4.Data.Color
                    {
                        Red = 1f,
                        Green = 1f,
                        Blue = 1f,
                    },
                    6 => new Google.Apis.Sheets.v4.Data.Color
                    {
                        Red = 1f,
                        Green = 1f,
                        Blue = 1f,
                    },
                    _ => new Google.Apis.Sheets.v4.Data.Color
                    {
                        Red = 0f,
                        Green = 0f,
                        Blue = 0f,
                    }
                };

            // Add the appropriate colours and text to the sheet
            for (int i = 0; i < warningsToGive; i++)
            {
                requests.Add(new Request()
                {
                    RepeatCell = new RepeatCellRequest()
                    {
                        Range = new GridRange()
                        {
                            SheetId = sheetId,
                            StartColumnIndex = currentWarnings + i + 1,
                            EndColumnIndex = currentWarnings + i + 2,
                            StartRowIndex = rowIndex - 1,
                            EndRowIndex = rowIndex
                        },
                        Cell = new CellData()
                        {
                            UserEnteredFormat = new CellFormat()
                            {
                                BackgroundColor = GetCellBackgroundColour(currentWarnings + i + 1),
                                TextFormat = new TextFormat()
                                {
                                    ForegroundColor = GetCellForegroundColour(currentWarnings + i + 1)
                                }
                            },
                            UserEnteredValue = new ExtendedValue()
                            {
                                StringValue = warningCode
                            }
                        },
                        Fields = "*"
                    }
                });
            }

            requests.Add(new Request()
            {
                RepeatCell = new RepeatCellRequest()
                {
                    Range = new GridRange()
                    {
                        SheetId = sheetId,
                        StartColumnIndex = 9,
                        EndColumnIndex = 10,
                        StartRowIndex = rowIndex - 1,
                        EndRowIndex = rowIndex
                    },
                    Cell = new CellData()
                    {
                        UserEnteredValue = new ExtendedValue()
                        {
                            StringValue = currentWarnings < 3 ? "Normal" : currentWarnings == 6 ? "Banned" : "Criminal"
                        }
                    },
                    Fields = "*"
                }
            });

            requests.Add(new Request()
            {
                RepeatCell = new RepeatCellRequest()
                {
                    Range = new GridRange()
                    {
                        SheetId = sheetId,
                        StartColumnIndex = 9,
                        EndColumnIndex = 10,
                        StartRowIndex = rowIndex - 1,
                        EndRowIndex = rowIndex
                    },
                    Cell = new CellData()
                    {
                        UserEnteredValue = new ExtendedValue()
                        {
                            NumberValue = totalWarnings
                        }
                    },
                    Fields = "*"
                }
            });

            await service.Spreadsheets.BatchUpdate(new BatchUpdateSpreadsheetRequest
            {
                Requests = requests
            }, spreadsheetId).ExecuteAsync();
        }

        [Command("removerole")]
        [Summary("Removes a role from the role list.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task RemoveRoleCommand([Remainder] string searchTerm)
        {
            var guild = GetGuild(Context);
            var id = Context.Guild.Roles.Where(r => string.Compare(r.Name, searchTerm, true) == 0).First().Id;
            if (!Context.Guild.Roles.Any(r => string.Compare(r.Name, searchTerm, true) == 0) ||
                !guild.Roles.Contains(id))
            {
                await SendErrorAsync("Could not find the role!");
                return;
            }
            guild.Roles.Remove(id);
            WriteGuilds(GetGuildsObject(), Context.Guild, guild);
            await ReplyAsync("Succesfully updated.");
        }

        [Command("setchannel")]
        [Summary("Sets a channel.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task SetChannelAsync(string option, string rawChannel)
        {
            if (!ulong.TryParse(rawChannel.RemoveChar('<').RemoveChar('>').RemoveChar('#'), out ulong channel))
            {
                await ReplyAsync("Invalid channel!");
                return;
            }
            var guild = GetGuild(Context);
            switch (option.ToLower().RemoveChar(' '))
            {
                case "announcement": guild.AnnouncementChannel = channel; break;
                case "autoresponse": guild.AutoresponseChannel = channel; break;
                case "usable": guild.UsableChannels.Add(channel); break;
                default: await ReplyAsync("Invalid option. Use `mb/help setchannel` for more info."); return;
            }
            WriteGuilds(GetGuildsObject(), Context.Guild, guild);
            await ReplyAsync("Successfully updated.");
        }

        [Command("setcolor")]
        [Alias("setcolour", "setembedcolor", "setembedcolour")]
        [Summary("Sets the embed colour of the guild using a hexadecimal colour string.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task SetColorCommand(string input)
        {
            if (!uint.TryParse(input, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
            {
                await ReplyAsync("Invalid hexadecimal colour string.");
                return;
            }
            var guild = GetGuild(Context);
            guild.Color = input;
            WriteGuilds(GetGuildsObject(), Context.Guild, guild);
            await ReplyAsync("Successfully updated.");
        }

        [Command("setprefix")]
        [Summary("Sets the bot prefix for the current guild.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task SetPrefix(string prefix)
        {
            var guild = GetGuild(Context);
            guild.Prefix = prefix;
            WriteGuilds(GetGuildsObject(), Context.Guild, guild);
            await ReplyAsync($"Successfully updated MarbleBot's prefix for **{Context.Guild.Name}** to **{prefix}**.");
        }

        [Command("setslowmode")]
        [Summary("Sets the slowmode interval of a channel (in seconds).")]
        [RequireBotPermission(ChannelPermission.ManageChannels)]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task SetSlowmodeCommand(Discord.WebSocket.SocketTextChannel textChannel, int slowmodeInterval)
        {
            await textChannel.ModifyAsync(c => c.SlowModeInterval = slowmodeInterval);
            await ReplyAsync($"Successfully updated the slowmode interval of <#{textChannel.Id}> to **{slowmodeInterval}** second{(slowmodeInterval == 1 ? "" : "s")}.");
        }

        [Command("warn", RunMode = RunMode.Async)]
        [Summary("Warns a user.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task WarnCommand(IGuildUser user, string warningCode, int warningsToGive)
            => await WarnUserAsync(Context, user, warningCode, warningsToGive);

        [Command("warn", RunMode = RunMode.Async)]
        [Summary("Warns a user.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task WarnCommand(ulong userId, string warningCode, int warningsToGive)
        {
            if (Context.Guild.Users.Any(u => u.Id == userId))
                await WarnUserAsync(Context, Context.Guild.GetUser(userId), warningCode, warningsToGive);
            else
                await SendErrorAsync("Could not find a user to warn!");
        }

        public async Task WarnUserAsync(SocketCommandContext context, IGuildUser user, string warningCode, int warningsToGive)
        {
            if (warningsToGive < 1)
            {
                await SendErrorAsync("The number of warnings issued must be at least 1!");
                return;
            }

            string pluralChar = warningsToGive > 1 ? "s" : "";

            await ReplyAsync(embed: new EmbedBuilder()
                .WithAuthor(context.User)
                .WithColor(Discord.Color.Red)
                .WithCurrentTimestamp()
                .WithDescription($"**{user}** has been given **{warningsToGive}** warning{pluralChar} for violating rule **{warningCode}**.")
                .WithFooter("If you believe this was a mistake, please contact a staff member.")
                .WithTitle($":warning: Warning{pluralChar} Issued :warning:")
                .Build());

            await (await user.GetOrCreateDMChannelAsync()).SendMessageAsync(embed: new EmbedBuilder()
                .WithAuthor(context.User)
                .WithColor(Discord.Color.Red)
                .WithCurrentTimestamp()
                .WithDescription($"You have been given **{warningsToGive}** warning{pluralChar} for violating rule **{warningCode}**.")
                .WithFooter("If you believe this was a mistake, please contact a staff member.")
                .WithTitle($":warning: Warning{pluralChar} Issued :warning:")
                .Build());

            await LogWarningAsync(user, warningCode, warningsToGive);
        }
    }
}
