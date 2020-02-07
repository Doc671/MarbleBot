using Discord;
using Discord.Commands;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MarbleBot.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Modules
{
    [Summary("YouTube API-related commands.")]
    public class YouTube : MarbleBotModule
    {
        private readonly BotCredentials _botCredentials;

        public YouTube(BotCredentials botCredentials)
        {
            _botCredentials = botCredentials;
        }

        [Command("channelinfo")]
        [Summary("Returns information about a channel.")]
        public async Task ChannelInfoCommand([Remainder] string searchTerm)
        {
            Channel channelListResult;

            using (var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = _botCredentials.GoogleApiKey,
                ApplicationName = GetType().ToString()
            }))
            {
                SearchResource.ListRequest searchListRequest = youtubeService.Search.List("snippet");
                searchListRequest.Q = searchTerm;
                searchListRequest.MaxResults = 1;
                var searchListResult = (await searchListRequest.ExecuteAsync()).Items.FirstOrDefault();

                if (searchListResult == null)
                {
                    await SendErrorAsync("Could not find the requested channel!");
                    return;
                }

                ChannelsResource.ListRequest channelListRequest = youtubeService.Channels.List("snippet");
                channelListRequest.Id = searchListResult.Snippet.ChannelId;
                channelListRequest.MaxResults = 10;
                channelListResult = (await channelListRequest.ExecuteAsync()).Items.FirstOrDefault();

                if (channelListResult == null)
                {
                    await SendErrorAsync("Could not find the requested channel!");
                    return;
                }
            }

            var builder = new EmbedBuilder()
                .WithTitle(channelListResult.Snippet.Title)
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithFooter(channelListResult.Id)
                .WithThumbnailUrl(channelListResult.Snippet.Thumbnails.Medium.Url);

            if (!string.IsNullOrEmpty(channelListResult.Snippet.Description))
            {
                builder.AddField("Description", channelListResult.Snippet.Description);
            }

            if (channelListResult.Snippet.Country != null)
            {
                builder.AddField("Country", channelListResult.Snippet.Country, true);
            }

            if (channelListResult.Snippet.PublishedAt != null)
            {
                builder.AddField("Created", channelListResult.Snippet.PublishedAt.Value.ToString("yyyy-MM-dd HH:mm:ss"), true);
            }

            await ReplyAsync(embed: builder.Build());
        }

        [Command("cv")]
        [Summary("Allows verified users to send a video in Community Marble channel #community-videos.")]
        [Remarks("CM Only")]
        [RequireContext(ContextType.DM)]
        public async Task CommunityVideosCommand(string url, [Remainder] string desc = "")
        {
            var validUser = false;
            var channelId = "";
            using (var CVID = new StreamReader($"Resources{Path.DirectorySeparatorChar}CVID.csv"))
            {
                while (!CVID.EndOfStream && !validUser)
                {
                    var person = (await CVID.ReadLineAsync())!.Split(',');
                    if (Context.User.Id == Convert.ToUInt64(person[0]))
                    {
                        validUser = true;
                        channelId = person[1];
                    }
                }
            }

            if (!validUser)
            {
                await ReplyAsync(new StringBuilder("It doesn't look like you're allowed to post in <#442474624417005589>.\n\n")
                  .Append("If you have more than 25 subs, post reasonable Algodoo-related content and are in good standing with the rules, sign up here: https://goo.gl/forms/opPSzUg30BECNku13 \n\n")
                  .Append("If you're an accepted user, please notify Doc671.")
                  .ToString());
                return;
            }

            using var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = _botCredentials.GoogleApiKey,
                ApplicationName = GetType().ToString()
            });

            string videoId = url.Contains("https://www.youtube.com/watch?v=")
                ? url.Remove(0, 32)     // removes "https://www.youtube.com/watch?v="
                : url.Remove(0, 17);    // removes "https://youtu.be/"

            var videoListRequest = youtubeService.Videos.List("snippet");
            videoListRequest.Id = videoId;
            var videoListResponse = await videoListRequest.ExecuteAsync();
            var video = videoListResponse.Items.First();

            if (video.Snippet.ChannelId != channelId)
            {
                await ReplyAsync("One of the following occured:\n\n- This isn't your video.\n- Your video could not be found.\n\nPlease notify Doc671 of this.");
                return;
            }

            if (DateTime.Now.Subtract((DateTime)video.Snippet.PublishedAt!).Days > 1)
            {
                await SendErrorAsync("The video cannot be more than two days old!");
                return;
            }

            if (desc.Length > 200)
            {
                await ReplyAsync("Your description length is too long!");
                return;
            }

            var communityVideoChannel = (IMessageChannel)Context.Client.GetChannel(442474624417005589);
            var msgs = await communityVideoChannel.GetMessagesAsync(100).FlattenAsync();
            var alreadyPosted = false;

            foreach (var msg in msgs)
            {
                if (msg.Content.Contains(url))
                {
                    alreadyPosted = true;
                    break;
                }
            }

            if (alreadyPosted)
            {
                await ReplyAsync("This video has already been posted!");
            }
            else
            {
                await communityVideoChannel.SendMessageAsync($"{desc}\n{url}");
            }
        }

        [Command("searchchannel")]
        [Summary("Displays a list of channels that match the search criteria.")]
        public async Task SearchChannelCommand([Remainder] string searchTerm)
        {
            SearchResource.ListRequest searchListRequest;

            using var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = _botCredentials.GoogleApiKey,
                ApplicationName = GetType().ToString()
            });

            searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = searchTerm;
            searchListRequest.MaxResults = 10;

            // Call the search.list method to retrieve results matching the specified query term.
            var searchListResponse = await searchListRequest.ExecuteAsync();

            var channels = new List<string>();

            // Add each result to the appropriate list, and then display the lists of
            // matching channels.
            bool found = false;

            foreach (var searchResult in searchListResponse.Items)
            {
                if (string.Compare(searchResult.Id.Kind, "youtube#channel", true) == 0 && !await Moderation.CheckSwearAsync(searchResult.Snippet.Title))
                {
                    channels.Add($"{searchResult.Snippet.Title} (<https://www.youtube.com/channel/{searchResult.Id.ChannelId}>)");
                    found = true;
                }
            }

            if (found)
            {
                await ReplyAsync($"**__Channels:__**\n{string.Join("\n", channels)}\n");
            }
            else
            {
                await SendErrorAsync("Couldn't seem to find anything...");
            }
        }


        [Command("searchvideo")]
        [Summary("Displays a list of videos that match the search critera.")]
        public async Task SearchVideoCommand([Remainder] string searchTerm)
        {
            SearchResource.ListRequest searchListRequest;

            using var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = _botCredentials.GoogleApiKey,
                ApplicationName = GetType().ToString()
            });

            searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = searchTerm;
            searchListRequest.MaxResults = 10;

            // Call the search.list method to retrieve results matching the specified query term.
            var searchListResponse = await searchListRequest.ExecuteAsync();

            var videos = new List<string>();

            // Add each result to the appropriate list, and then display the lists of
            // matching videos.
            bool found = false;
            foreach (var searchResult in searchListResponse.Items)
            {
                if (string.Compare(searchResult.Id.Kind, "youtube#video", true) == 0 && !await Moderation.CheckSwearAsync(searchResult.Snippet.Title))
                {
                    videos.Add($"{searchResult.Snippet.Title} (<https://youtu.be/{searchResult.Id.VideoId}>)");
                    found = true;
                }
            }

            if (found)
            {
                await ReplyAsync($"**__Videos__**:\n{string.Join("\n", videos)}\n");
            }
            else
            {
                await ReplyAsync("Couldn't seem to find anything...");
            }
        }
    }
}
