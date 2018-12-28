using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace MarbleBot.Modules
{

    public class YT : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// YouTube API-related commands
        /// </summary>

        [Command("channelinfo")]
        [Summary("returns information about a channel")]
        public async Task _channelinfo([Remainder] string searchTerm)
        {
            await Context.Channel.TriggerTypingAsync();
            Channel display = new Channel();

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = Global.YTKey,
                ApplicationName = GetType().ToString()
            });

            var searchChannels = youtubeService.Channels.List(searchTerm).Execute();
            display = searchChannels.Items[0];

            EmbedBuilder builder = new EmbedBuilder();
            Color coloure = Color.LightGrey;
            switch (Context.Guild.Id)
            {
                case Global.CM: coloure = Color.Teal; break;
                case Global.THS: coloure = Color.Orange; break;
                case Global.MT: coloure = Color.DarkGrey; break;
                case Global.VFC: coloure = Color.Blue; break;
                case Global.THSC: coloure = Color.Orange; break;
            }

            builder.WithTitle(display.Snippet.Title)
                .WithColor(coloure)
                .WithCurrentTimestamp()
                .WithFooter(display.ETag)
                .WithThumbnailUrl(display.Snippet.Thumbnails.Default__.Url)
                .AddInlineField("Subscribers", display.Statistics.SubscriberCount)
                .AddInlineField("Views", display.Statistics.ViewCount)
                .AddInlineField("Videos", display.Statistics.VideoCount)
                .AddInlineField("Country", display.Snippet.Country)
                .AddInlineField("Description", display.Snippet.Description);

            await ReplyAsync("", false, builder.Build());
        }

        [Command("cv")]
        [Summary("Allows verified users to send a video in CM #community-videos")]
        public async Task _cv(string url, [Remainder] string desc = "")
        {
            if (Context.IsPrivate)
            {
                var validUser = false;
                var channelLink = "";
                using (var CVID = new StreamReader("CVID.csv")) {
                    while (!CVID.EndOfStream) {
                        var person = (await CVID.ReadLineAsync()).Split(',');
                        if (Context.User.Id == Convert.ToUInt64(person[0])) {
                            validUser = true;
                            channelLink = person[1];
                            break;
                        }
                    }
                }
                if (validUser) {
                    var youtubeService = new YouTubeService(new BaseClientService.Initializer() {
                        ApiKey = Global.YTKey,
                        ApplicationName = GetType().ToString()
                    });
                    var searchListRequest = youtubeService.Search.List("snippet");
                    searchListRequest.Q = channelLink;
                    searchListRequest.MaxResults = 10;
                    var searchListResponse = await searchListRequest.ExecuteAsync();
                    var channel = new SearchResultSnippet();
                    foreach (var e in searchListResponse.Items) {
                        if (e.Id.Kind == "youtube#channel") channel = e.Snippet;
                    }
                    if (channel == null) {
                        searchListRequest.Q = Context.User.Username;
                        foreach (var e in searchListResponse.Items) {
                            if (e.Id.Kind == "youtube#channel") channel = e.Snippet;
                        }
                    }
                    searchListRequest.Q = url;
                    searchListResponse = await searchListRequest.ExecuteAsync();
                    var video = searchListResponse.Items[0].Snippet;
                    if (channel.Title == video.ChannelTitle) {
                        if (DateTime.Now.Subtract((DateTime)video.PublishedAt).Days > 1) {
                            await ReplyAsync("The video cannot be more than two days old!");
                        } else {
                            if (desc.Length > 200) await ReplyAsync("Your description length is too long!");
                            else {
                                var CV = (IMessageChannel)Context.Client.GetGuild(Global.CM).GetChannel(442474624417005589);
                                var msgs = await CV.GetMessagesAsync(100).Flatten();
                                var already = false;
                                foreach (var msg in msgs) {
                                    if (msg.Content.Contains(url)) already = true; break;
                                }
                                if (already) await ReplyAsync("This video has already been posted!");
                                else
                                {
                                    await CV.SendMessageAsync(desc + "\n" + url);
                                }
                            }
                        }
                    } else await ReplyAsync("One of the following occured:\n\n- This isn't your video.\n- Your video could not be found.\n- Your channel could not be found.\n- The wrong channel was found.\n\nPlease notify Doc671 of this.");
                    if (!validUser) Console.WriteLine("[0]: Failed operation of mb/cv. Channel Title: {1}; Video Channel Title: {2}.", DateTime.UtcNow, channel.Title, video.ChannelTitle);
                } else {
                    var output = "It doesn't look like you're allowed to post in <#442474624417005589>.\n\n";
                    output += "If you have more than 25 subs, post reasonable Algodoo-related content and are in good standing with the rules, sign up here: https://goo.gl/forms/opPSzUg30BECNku13 \n\n";
                    output += "If you're an accepted user, please notify Doc671.";
                    await ReplyAsync(output);
                }
            }
        }

        [Command("searchchannel")]
        [Summary("searches channels")]
        public async Task _searchchannel([Remainder] string searchTerm)
        {
            await Context.Channel.TriggerTypingAsync();
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = Global.YTKey,
                ApplicationName = GetType().ToString()
            });

            byte profaneCount = 0;

            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = searchTerm;
            searchListRequest.MaxResults = 10;

            // Call the search.list method to retrieve results matching the specified query term.
            var searchListResponse = await searchListRequest.ExecuteAsync();

            //List<string> videos = new List<string>();
            List<string> channels = new List<string>();
            //List<string> playlists = new List<string>();

            // Add each result to the appropriate list, and then display the lists of
            // matching videos, channels, and playlists.
            bool found = false;

            foreach (var searchResult in searchListResponse.Items)
            {
                if (searchResult.Id.Kind == "youtube#channel" && !Moderation._checkSwear(searchResult.Snippet.Title))
                {
                    channels.Add(String.Format("{0} (<https://www.youtube.com/channel/{1}>)", searchResult.Snippet.Title, searchResult.Id.ChannelId));
                    found = true;
                }
                else
                {
                    //channels.Add("(profanity detected, item not displayed)");
                    profaneCount++;
                }
            }

            if (found) {
                if (profaneCount > 0) {
                    await ReplyAsync(String.Format("**__Channels:__**\n{0}\n", string.Join("\n", channels)) + "\n" + profaneCount + " results omitted (profanity detected)");
                } else {
                    await ReplyAsync(String.Format("**__Channels:__**\n{0}\n", string.Join("\n", channels)));
                }
            } else {
                await ReplyAsync("Couldn't seem to find anything...");
            }
        }


        [Command("searchvideo")]
        [Summary("searches videos")]
        public async Task _searchvideo([Remainder] string searchTerm)
        {
            await Context.Channel.TriggerTypingAsync();
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = Global.YTKey,
                ApplicationName = GetType().ToString()
            });

            byte profaneCount = 0;

            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = searchTerm;
            searchListRequest.MaxResults = 10;

            // Call the search.list method to retrieve results matching the specified query term.
            var searchListResponse = await searchListRequest.ExecuteAsync();

            List<string> videos = new List<string>();

            // Add each result to the appropriate list, and then display the lists of
            // matching videos, channels, and playlists.
            bool found = false;
            foreach (var searchResult in searchListResponse.Items)
            {
                if (searchResult.Id.Kind == "youtube#video" && Moderation._checkSwear(searchResult.Snippet.Title)) {
                    //videos.Add("(profanity detected, item not displayed)");
                    profaneCount++;
                } else if (searchResult.Id.Kind == "youtube#video") {
                    videos.Add(String.Format("{0} (<https://youtu.be/{1}>)", searchResult.Snippet.Title, searchResult.Id.VideoId));
                    found = true;
                }
            }

            if (found) {
                if (profaneCount > 0) {
                    await ReplyAsync(String.Format("**__Videos__**:\n{0}\n", string.Join("\n", videos)) + "\n" + profaneCount + " results omitted (profanity detected)");
                } else {
                    await ReplyAsync(String.Format("**__Videos__**:\n{0}\n", string.Join("\n", videos)));
                }
            } else {
                await ReplyAsync("Couldn't seem to find anything...");
            }
        }
    }
}
