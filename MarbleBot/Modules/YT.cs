using System;
using System.Collections.Generic;
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
                ApiKey = "AIzaSyCa6hUsjY_2pt0ZrxxTNtfYE-BBUci3Jhg",
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


        [Command("searchchannel")]
        [Summary("searches channels")]
        public async Task _searchchannel([Remainder] string searchTerm)
        {
            await Context.Channel.TriggerTypingAsync();
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = "AIzaSyCa6hUsjY_2pt0ZrxxTNtfYE-BBUci3Jhg",
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
                ApiKey = "AIzaSyCa6hUsjY_2pt0ZrxxTNtfYE-BBUci3Jhg",
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
