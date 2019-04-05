using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace MarbleBot.Modules
{
    /// <summary> YouTube API-related commands. </summary>
    public class YouTube : MarbleBotModule
    {
        [Command("channelinfo")]
        [Summary("[BROKEN] Returns information about a channel.")]
        public async Task ChannelInfoCommandAsync([Remainder] string searchTerm)
        {
            await Context.Channel.TriggerTypingAsync();
            Channel display;

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
                case CM: coloure = Color.Teal; break;
                case THS: coloure = Color.Orange; break;
                case MT: coloure = Color.DarkGrey; break;
                case VFC: coloure = Color.Blue; break;
                case THSC: coloure = Color.Orange; break;
            }

            builder.WithTitle(display.Snippet.Title)
                .WithColor(coloure)
                .WithCurrentTimestamp()
                .WithFooter(display.ETag)
                .WithThumbnailUrl(display.Snippet.Thumbnails.Default__.Url)
                .AddField("Subscribers", display.Statistics.SubscriberCount, true)
                .AddField("Views", display.Statistics.ViewCount, true)
                .AddField("Videos", display.Statistics.VideoCount, true)
                .AddField("Country", display.Snippet.Country, true)
                .AddField("Description", display.Snippet.Description, true);

            await ReplyAsync(embed: builder.Build());
        }

        [Command("cv")]
        [Summary("Allows verified users to send a video in Community Marble channel #community-videos.")]
        [Remarks("CM Only")]
        public async Task CommunityVideosCommandAsync(string url, [Remainder] string desc = "")
        {
            if (Context.IsPrivate)
            {
                var validUser = false;
                var channelLink = "";
                using (var CVID = new StreamReader("Resources\\CVID.csv")) {
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
                                var CV = (IMessageChannel)Context.Client.GetGuild(CM).GetChannel(442474624417005589);
                                var msgs = await CV.GetMessagesAsync(100).FlattenAsync();
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
                    if (!validUser) Trace.WriteLine($"[{DateTime.UtcNow}]: Failed operation of mb/cv. Channel Title: {channel.Title}; Video Channel Title: {video.ChannelTitle}.");
                } else {
                    var output = new StringBuilder("It doesn't look like you're allowed to post in <#442474624417005589>.\n\n");
                    output.Append("If you have more than 25 subs, post reasonable Algodoo-related content and are in good standing with the rules, sign up here: https://goo.gl/forms/opPSzUg30BECNku13 \n\n");
                    output.Append("If you're an accepted user, please notify Doc671.");
                    await ReplyAsync(output.ToString());
                }
            }
        }

        [Command("searchchannel")]
        [Summary("Displays a list of channels that match the search criteria.")]
        public async Task SearchChannelCommandAsync([Remainder] string searchTerm)
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
                if (searchResult.Id.Kind == "youtube#channel" && !Moderation.CheckSwear(searchResult.Snippet.Title))
                {
                    channels.Add(string.Format("{0} (<https://www.youtube.com/channel/{1}>)", searchResult.Snippet.Title, searchResult.Id.ChannelId));
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
                    await ReplyAsync(string.Format("**__Channels:__**\n{0}\n", string.Join("\n", channels)) + "\n" + profaneCount + " results omitted (profanity detected)");
                } else {
                    await ReplyAsync(string.Format("**__Channels:__**\n{0}\n", string.Join("\n", channels)));
                }
            } else {
                await ReplyAsync("Couldn't seem to find anything...");
            }
        }


        [Command("searchvideo")]
        [Summary("Displays a list of videos that match the search critera.")]
        public async Task SearchVideoCommandAsync([Remainder] string searchTerm)
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
                if (searchResult.Id.Kind == "youtube#video" && Moderation.CheckSwear(searchResult.Snippet.Title)) {
                    //videos.Add("(profanity detected, item not displayed)");
                    profaneCount++;
                } else if (searchResult.Id.Kind == "youtube#video") {
                    videos.Add(string.Format("{0} (<https://youtu.be/{1}>)", searchResult.Snippet.Title, searchResult.Id.VideoId));
                    found = true;
                }
            }

            if (found) {
                if (profaneCount > 0) {
                    await ReplyAsync(string.Format("**__Videos__**:\n{0}\n", string.Join("\n", videos)) + "\n" + profaneCount + " results omitted (profanity detected)");
                } else {
                    await ReplyAsync(string.Format("**__Videos__**:\n{0}\n", string.Join("\n", videos)));
                }
            } else {
                await ReplyAsync("Couldn't seem to find anything...");
            }
        }
    }
}
