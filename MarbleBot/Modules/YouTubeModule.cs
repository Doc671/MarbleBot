using Google.Apis.YouTube.v3.Data;
using MarbleBot.Services;

namespace MarbleBot.Modules;

public class YouTubeModule : MarbleBotSlashCommandModule
{
    private readonly YouTubeApiService _youTubeApiService;
    private readonly AutoModFilterService _autoModFilterService;

    private const int _maxDescriptionLength = 300;

    public YouTubeModule(YouTubeApiService youTubeApiService, AutoModFilterService autoModFilterService)
    {
        _youTubeApiService = youTubeApiService;
        _autoModFilterService = autoModFilterService;
    }

    private bool ShouldEnforceSafeSearch()
    {
        if (Context.Guild == null)
        {
            return false;
        }

        if (Context.Guild.ExplicitContentFilter == ExplicitContentFilterLevel.Disabled)
        {
            return false;
        }

        if (Context.Channel is ITextChannel textChannel && textChannel.IsNsfw)
        {
            return false;
        }

        return true;
    }

    [SlashCommand("channelinfo", "Returns information about a YouTube channel.")]
    public async Task ChannelInfoCommand(string searchTerm)
    {
        if (_youTubeApiService == null)
        {
            await RespondWithErrorAsync("YouTube API service is not available.");
            return;
        }

        bool enforceSafeSearch = ShouldEnforceSafeSearch();

        Channel? channel = await _youTubeApiService.GetChannelAsync(searchTerm, enforceSafeSearch, true);
        if (channel == null)
        {
            string filteredSearchTerm = await _autoModFilterService.FilterAsync(searchTerm, Context);
            await RespondWithErrorAsync($"Could not find the requested channel \"{filteredSearchTerm}\".");
            return;
        }

        string title = await _autoModFilterService.FilterAsync(channel.Snippet.Title, Context);
        var builder = new EmbedBuilder()
            .WithTitle(title)
            .WithThumbnailUrl(channel.Snippet.Thumbnails.Medium.Url)
            .WithUrl($"https://www.youtube.com/channel/{channel.Id}")
            .WithFooter(channel.Id);

        string description = channel.Snippet.Description;
        if (!string.IsNullOrEmpty(description))
        {
            if (description.Length > _maxDescriptionLength)
            {
                description = description[.._maxDescriptionLength] + "...";
            }

            description = await _autoModFilterService.FilterAsync(description, Context);
            builder.WithDescription(description);
        }

        string publishedAtRaw = channel.Snippet.PublishedAtRaw;
        if (publishedAtRaw != null && DateTimeOffset.TryParse(publishedAtRaw, out DateTimeOffset publishedAt))
        {
            long unixTime = publishedAt.ToUnixTimeSeconds();
            builder.AddField("Created", $"<t:{unixTime}:S>", true);
        }

        if (channel.Snippet.Country != null)
        {
            builder.AddField("Country", channel.Snippet.Country, true);
        }

        if (channel.Statistics?.SubscriberCount != null)
        {
            builder.AddField("Subscribers", channel.Statistics.SubscriberCount.Value.ToString("N0"), true);
        }

        if (channel.Statistics?.ViewCount != null)
        {
            builder.AddField("Views", channel.Statistics.ViewCount.Value.ToString("N0"), true);
        }

        if (channel.Statistics?.VideoCount != null)
        {
            builder.AddField("Videos", channel.Statistics.VideoCount.Value.ToString("N0"), true);
        }

        await RespondAsync(embed: builder.Build());
    }

    [SlashCommand("searchchannel", "Returns a list of channels matching the search criteria.")]
    public async Task SearchChannelCommand(string searchTerm)
    {
        if (_youTubeApiService == null)
        {
            await RespondWithErrorAsync("YouTube API service is not available.");
            return;
        }

        bool enforceSafeSearch = ShouldEnforceSafeSearch();

        List<SearchResult>? channels = await _youTubeApiService.SearchAsync(searchTerm, $"youtube#channel", enforceSafeSearch);

        string filteredSearchTerm = await _autoModFilterService.FilterAsync(searchTerm, Context);
        if (channels == null || channels.Count == 0)
        {
            await RespondWithErrorAsync($"Could not find any channels matching \"{filteredSearchTerm}\".");
            return;
        }

        string searchResults = string.Join("\n", channels.Select(channel => $"[{channel.Snippet.Title}](https://www.youtube.com/channel/{channel.Snippet.ChannelId})"));
        searchResults = await _autoModFilterService.FilterAsync(searchResults, Context);

        var builder = new EmbedBuilder()
            .WithTitle($"Search results for \"{filteredSearchTerm}\"")
            .WithDescription(searchResults);

        await RespondAsync(embed: builder.Build());
    }

    [SlashCommand("searchvideo", "Returns a list of videos matching the search criteria.")]
    public async Task SearchVideoCommand(string searchTerm)
    {
        if (_youTubeApiService == null)
        {
            await RespondWithErrorAsync("YouTube API service is not available.");
            return;
        }

        bool enforceSafeSearch = ShouldEnforceSafeSearch();

        List<SearchResult>? videos = await _youTubeApiService.SearchAsync(searchTerm, $"youtube#video", enforceSafeSearch);

        string filteredSearchTerm = await _autoModFilterService.FilterAsync(searchTerm, Context);
        if (videos == null || videos.Count == 0)
        {
            await RespondWithErrorAsync($"Could not find any videos matching \"{filteredSearchTerm}\".");
            return;
        }

        string searchResults = string.Join("\n", videos.Select(video => $"[{video.Snippet.Title}](https://www.youtube.com/watch?v={video.Id.VideoId}) by **{video.Snippet.ChannelTitle}**"));
        searchResults = await _autoModFilterService.FilterAsync(searchResults, Context);

        var builder = new EmbedBuilder()
            .WithTitle($"Search results for \"{filteredSearchTerm}\"")
            .WithDescription(searchResults);

        await RespondAsync(embed: builder.Build());
    }

    [SlashCommand("videoinfo", "Returns information about a YouTube video.")]
    public async Task VideoInfoCommand(string searchTerm)
    {
        if (_youTubeApiService == null)
        {
            await RespondWithErrorAsync("YouTube API service is not available.");
            return;
        }

        bool enforceSafeSearch = ShouldEnforceSafeSearch();

        Video? video = await _youTubeApiService.GetVideoAsync(searchTerm, enforceSafeSearch, true);
        if (video == null)
        {
            string filteredSearchTerm = await _autoModFilterService.FilterAsync(searchTerm, Context);
            await RespondWithErrorAsync($"Could not find the requested video \"{filteredSearchTerm}\".");
            return;
        }

        if (enforceSafeSearch && video.AgeGating.Restricted != null && video.AgeGating.Restricted.Value)
        {
            await RespondWithErrorAsync("The requested video is age-restricted and cannot be displayed in this channel.");
            return;
        }

        string title = await _autoModFilterService.FilterAsync(video.Snippet.Title, Context);
        var builder = new EmbedBuilder()
            .WithTitle(title)
            .WithThumbnailUrl(video.Snippet.Thumbnails.Medium.Url)
            .WithUrl($"https://www.youtube.com/watch?v={video.Id}")
            .WithFooter(video.Id);

        string description = video.Snippet.Description;
        if (!string.IsNullOrEmpty(description))
        {
            if (description.Length > _maxDescriptionLength)
            {
                description = description[.._maxDescriptionLength] + "...";
            }

            description = await _autoModFilterService.FilterAsync(description, Context);
            builder.WithDescription(description);
        }

        builder.AddField("Channel", $"[{video.Snippet.ChannelTitle}](https://www.youtube.com/channel/{video.Snippet.ChannelId})", true);

        string publishedAtRaw = video.Snippet.PublishedAtRaw;
        if (publishedAtRaw != null && DateTimeOffset.TryParse(publishedAtRaw, out DateTimeOffset publishedAt))
        {
            long unixTime = publishedAt.ToUnixTimeSeconds();
            builder.AddField("Created", $"<t:{unixTime}:S>", true);
        }

        if (video.Statistics.ViewCount != null)
        {
            builder.AddField("Views", video.Statistics.ViewCount.Value.ToString("N0"), true);
        }

        if (video.Statistics.LikeCount != null)
        {
            builder.AddField("Likes", video.Statistics.LikeCount.Value.ToString("N0"), true);
        }

        if (video.Statistics.DislikeCount != null)
        {
            builder.AddField("Dislikes", video.Statistics.DislikeCount.Value.ToString("N0"), true);
        }

        if (video.Statistics.CommentCount != null)
        {
            builder.AddField("Comments", video.Statistics.CommentCount.Value.ToString("N0"), true);
        }

        await RespondAsync(embed: builder.Build());
    }
}
