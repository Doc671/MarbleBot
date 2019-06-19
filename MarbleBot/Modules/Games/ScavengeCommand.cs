using Discord;
using Discord.Commands;
using MarbleBot.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using static MarbleBot.Global;

namespace MarbleBot.Modules
{
    public partial class Games
    {
        [Group("scavenge")]
        [Summary("Scavenge for items!")]
        public class ScavengeCommand : MarbleBotModule
        {
            private const GameType Type = GameType.Race;

            public async Task ScavengeStartAsync(MBUser user, ScavengeLocation location)
            {
                await Context.Channel.TriggerTypingAsync();
                var embed = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp();

                if (DateTime.UtcNow.Subtract(user.LastScavenge).TotalHours < 6)
                {
                    var sixHoursAgo = DateTime.UtcNow.AddHours(-6);
                    await ReplyAsync($"**{Context.User.Username}**, you need to wait for **{GetDateString(user.LastScavenge.Subtract(sixHoursAgo))}** until you can scavenge again.");
                }
                else
                {
                    if (ScavengeInfo.ContainsKey(Context.User.Id)) await ReplyAsync($"**{Context.User.Username}**, you are already scavenging!");
                    else
                    {
                        ScavengeInfo.GetOrAdd(Context.User.Id, new Scavenge(Context, location));
                        embed.WithDescription($"**{Context.User.Username}** has begun scavenging in **{Enum.GetName(typeof(ScavengeLocation), location)}**!")
                            .WithTitle("Item Scavenge Begin!");
                        await ReplyAsync(embed: embed.Build());
                    }
                }
            }

            [Command("canarybeach")]
            [Alias("canary beach")]
            [Summary("Starts a scavenge session in Canary Beach.")]
            public async Task ScavengeCanaryCommandAsync()
                => await ScavengeStartAsync(GetUser(Context), ScavengeLocation.CanaryBeach);

            [Command("destroyersremains")]
            [Alias("destroyer'sremains", "destroyer's remains")]
            [Remarks("Stage2")]
            [Summary("Starts a scavenge session in Destroyer's Remains.")]
            public async Task ScavengeDestroyerCommandAsync()
            {
                if (GetUser(Context).Stage < 2) await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithDescription($"{StageTooHighString()}\n\nYou cannot scavenge in this location!")
                    .WithTitle("Scavenge: Destroyer's Remains")
                    .Build());
                else await ScavengeStartAsync(GetUser(Context), ScavengeLocation.DestroyersRemains);
            }

            [Command("treewurld")]
            [Alias("tree wurld")]
            [Summary("Starts a scavenge session in Tree Wurld.")]
            public async Task ScavengeTreeCommandAsync()
                => await ScavengeStartAsync(GetUser(Context), ScavengeLocation.TreeWurld);

            [Command("violetvolcanoes")]
            [Alias("violet volcanoes")]
            [Remarks("Stage2")]
            [Summary("Starts a scavenge session in the Violet Volcanoes.")]
            public async Task ScavengeVolcanoCommandAsync()
            {
                if (GetUser(Context).Stage < 2) await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithDescription($"{StageTooHighString()}\n\nYou cannot scavenge in this location!")
                    .WithTitle("Scavenge: Violet Volcanoes")
                    .Build());
                else await ScavengeStartAsync(GetUser(Context), ScavengeLocation.VioletVolcanoes);
            }

            [Command("grab")]
            [Alias("take")]
            [Summary("Grabs an item found in a scavenge session.")]
            public async Task ScavengeGrabCommandAsync()
            {
                await Context.Channel.TriggerTypingAsync();
                var obj = GetUsersObject();
                var user = GetUser(Context, obj);

                if (ScavengeInfo.ContainsKey(Context.User.Id))
                {
                    if (ScavengeInfo[Context.User.Id] == null) await ReplyAsync($"**{Context.User.Username}**, there is no item to grab!");
                    else
                    {
                        if (ScavengeInfo[Context.User.Id].Items.Count > 0)
                        {
                            if (ScavengeInfo[Context.User.Id].Items.Peek().Name.Contains("Ore"))
                                await ReplyAsync($"**{Context.User.Username}**, you need to drill for ore!");
                            else
                            {
                                var item = ScavengeInfo[Context.User.Id].Items.Dequeue();
                                if (user.Items != null)
                                {
                                    if (user.Items.ContainsKey(item.Id)) user.Items[item.Id]++;
                                    else user.Items.Add(item.Id, 1);
                                }
                                else
                                {
                                    user.Items = new SortedDictionary<int, int> {
                                        { item.Id, 1 }
                                    };
                                }
                                user.NetWorth += item.Price;
                                WriteUsers(obj, Context.User, user);
                                await ReplyAsync($"**{Context.User.Username}**, you have successfully added **{item.Name}** x**1** to your inventory!");
                            }
                        }
                        else await ReplyAsync($"**{Context.User.Username}**, there is nothing to grab!");
                    }
                }
                else await ReplyAsync($"**{Context.User.Username}**, you are not scavenging!");
            }

            [Command("drill")]
            [Alias("extract", "mine")]
            [Remarks("Stage2")]
            [Summary("Extracts an ore found in a scavenge session.")]
            public async Task ScavengeDrillCommandAsync()
            {
                await Context.Channel.TriggerTypingAsync();
                var obj = GetUsersObject();
                var user = GetUser(Context, obj);

                if (ScavengeInfo.ContainsKey(Context.User.Id))
                {
                    if (ScavengeInfo[Context.User.Id] == null) await ReplyAsync($"**{Context.User.Username}**, there is no item to drill!");
                    else
                    {
                        if (ScavengeInfo[Context.User.Id].Items.Count > 0)
                        {
                            if (ScavengeInfo[Context.User.Id].Items.Peek().Name.Contains("Ore"))
                            {
                                if (user.Items.ContainsKey(81) || user.Items.ContainsKey(82))
                                {
                                    var item = ScavengeInfo[Context.User.Id].Items.Dequeue();
                                    if (user.Items != null)
                                    {
                                        if (user.Items.ContainsKey(item.Id)) user.Items[item.Id]++;
                                        else user.Items.Add(item.Id, 1);
                                    }
                                    else
                                    {
                                        user.Items = new SortedDictionary<int, int> {
                                            { item.Id, 1 }
                                        };
                                    }
                                    user.NetWorth += item.Price;
                                    WriteUsers(obj, Context.User, user);
                                    await ReplyAsync($"**{Context.User.Username}**, you have successfully added **{item.Name}** x**1** to your inventory!");
                                }
                                else await ReplyAsync($"**{Context.User.Username}**, you need a drill to mine ore!");
                            }
                            else await ReplyAsync($"**{Context.User.Username}**, this is not an ore!");
                        }
                        else await ReplyAsync($"**{Context.User.Username}**, there is nothing to drill!");
                    }
                }
                else await ReplyAsync($"**{Context.User.Username}**, you are not scavenging!");
            }

            [Command("sell")]
            [Summary("Sells an item found in a scavenge session.")]
            public async Task ScavengeSellCommandAsync()
            {
                await Context.Channel.TriggerTypingAsync();
                var obj = GetUsersObject();
                var user = GetUser(Context, obj);

                if (ScavengeInfo.ContainsKey(Context.User.Id))
                {
                    if (ScavengeInfo[Context.User.Id] == null) await ReplyAsync($"**{Context.User.Username}**, there is no item to sell!");
                    else
                    {
                        if (ScavengeInfo[Context.User.Id].Items.Count > 0)
                        {
                            if (ScavengeInfo[Context.User.Id].Items.Peek().Name.Contains("Ore"))
                                await ReplyAsync($"**{Context.User.Username}**, you need to drill for ore!");
                            else
                            {
                                var item = ScavengeInfo[Context.User.Id].Items.Dequeue();
                                user.Balance += item.Price;
                                user.NetWorth += item.Price;
                                WriteUsers(obj, Context.User, user);
                                await ReplyAsync($"**{Context.User.Username}**, you have successfully sold **{item.Name}** x**1** for {UoM}**{item.Price:n2}**!");
                            }
                        }
                        else await ReplyAsync($"**{Context.User.Username}**, there is nothing to sell!");
                    }
                }
                else await ReplyAsync($"**{Context.User.Username}**, you are not scavenging!");
            }

            [Command("locations")]
            [Summary("Shows scavenge locations.")]
            public async Task ScavengeLocationCommandAsync()
            {
                await Context.Channel.TriggerTypingAsync();
                var stageTwoLocations = GetUser(Context).Stage > 1 ? "Destroyer's Remains\nViolet Volcanoes"
                    : $":lock: LOCKED\n:lock: LOCKED";
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithDescription($"Canary Beach\nTree Wurld\n{stageTwoLocations}\n\nUse `mb/scavenge info <location name>` to see which items you can get!")
                    .WithTitle("Scavenge Locations")
                    .Build());
            }

            [Command("next")]
            [Alias("checkearn")]
            [Summary("Shows whether you can scavenge and if not, when you will be able to.")]
            public async Task ScavengeCheckearnAsync()
            => await CheckearnAsync(Context, Type);

            public async Task ScavengeInfoAsync(ScavengeLocation location)
            {
                await Context.Channel.TriggerTypingAsync();
                var output = new StringBuilder();
                string json;
                using (var users = new StreamReader($"Resources{Path.DirectorySeparatorChar}Items.json")) json = users.ReadToEnd();
                var obj = JObject.Parse(json);
                var items = obj.ToObject<Dictionary<string, Item>>();
                foreach (var itemPair in items)
                {
                    if (itemPair.Value.ScavengeLocation == location)
                        output.AppendLine($"`[{int.Parse(itemPair.Key).ToString("000")}]` {itemPair.Value.Name}");
                }
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithDescription(output.ToString())
                    .WithTitle($"Scavenge Location Info: {Enum.GetName(typeof(ScavengeLocation), location)}")
                    .Build());
            }

            [Command("location canarybeach")]
            [Alias("location canary", "location canary beach", "info canarybeach", "info canary", "info canary beach")]
            [Summary("Shows scavenge location info for Canary Beach.")]
            public async Task ScavengeLocationCanaryCommandAsync()
                => await ScavengeInfoAsync(ScavengeLocation.CanaryBeach);

            [Command("location destroyersremains")]
            [Alias("location destroyer'sremains", "location destroyer's remains", "info destroyersremains", "info destroyer'sremains", "info destroyer's remains")]
            [Remarks("Stage2")]
            [Summary("Shows scavenge location info for Destroyer's Remains")]
            public async Task ScavengeLocationDestroyerCommandAsync()
            {
                if (GetUser(Context).Stage < 2) await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithDescription($"{StageTooHighString()}\n\nYou cannot scavenge in this location!")
                    .WithTitle("Scavenge Location Info: Destroyer's Remains")
                    .Build());
                else await ScavengeInfoAsync(ScavengeLocation.DestroyersRemains);
            }

            [Command("location treewurld")]
            [Alias("location tree", "location tree wurld", "info treewurld", "info tree", "info tree wurld")]
            [Summary("Shows scavenge location info for Tree Wurld")]
            public async Task ScavengeLocationTreeCommandAsync()
                => await ScavengeInfoAsync(ScavengeLocation.TreeWurld);

            [Command("location violetvolcanoes")]
            [Alias("location violet volcanoes", "info violetvolcanoes", "info violet volcanoes")]
            [Remarks("Stage2")]
            [Summary("Starts a scavenge session in Destroyer's Remains")]
            public async Task ScavengeLocationVolcanoCommandAsync()
            {
                if (GetUser(Context).Stage < 2) await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithDescription($"{StageTooHighString()}\n\nYou cannot scavenge in this location!")
                    .WithTitle("Scavenge Location Info: Violet Volcanoes")
                    .Build());
                else await ScavengeInfoAsync(ScavengeLocation.VioletVolcanoes);
            }

            [Command("")]
            [Alias("help")]
            [Priority(-1)]
            [Summary("Scavenge help.")]
            public async Task ScavengeHelpCommandAsync([Remainder] string _ = "")
            {
                await Context.Channel.TriggerTypingAsync();
                var helpP1 = "Use `mb/scavenge locations` to see where you can scavenge for items and use `mb/scavenge <location name>` to start a scavenge session!";
                var helpP2 = "\n\nWhen you find an item, use `mb/scavenge sell` to sell immediately or `mb/scavenge grab` to put the item in your inventory!";
                var helpP3 = "\n\nScavenge games last for 60 seconds - every 8 seconds there will be a 80% chance that you've found an item.";
                await ReplyAsync(embed: new EmbedBuilder()
                    .AddField("How to play", $"{helpP1} {helpP2} {helpP3}")
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithTitle("Item Scavenge!")
                    .Build());
            }
        }
    }
}
