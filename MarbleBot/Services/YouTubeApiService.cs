using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Reflection;

namespace MarbleBot.Services;

public class YouTubeApiService : IDisposable
{
    public bool IsAvailable => _youTubeService != null;

    private readonly YouTubeService? _youTubeService;

    public YouTubeApiService(BotCredentials botCredentials)
    {
        if (string.IsNullOrEmpty(botCredentials.GoogleApiKey))
        {
            return;
        }

        _youTubeService = new YouTubeService(new BaseClientService.Initializer
        {
            ApiKey = botCredentials.GoogleApiKey,
            ApplicationName = Assembly.GetExecutingAssembly().GetName().Name
        });
    }

    public async Task<Channel?> GetChannelAsync(string searchTerm, bool enforceSafeSearch = true, bool getStatistics = false)
    {
        SearchListResponse? searchListResponse = await GetSearchListAsync(searchTerm, enforceSafeSearch);
        if (searchListResponse == null)
        {
            return null;
        }

        SearchResult? searchListResult = searchListResponse.Items.FirstOrDefault(result => result.Id?.Kind == "youtube#channel" && !string.IsNullOrEmpty(result.Id?.ChannelId));
        if (searchListResult == null)
        {
            return null;
        }

        string parts = getStatistics ? "snippet,statistics" : "snippet";
        ChannelsResource.ListRequest channelListRequest = _youTubeService!.Channels.List(parts);

        channelListRequest.Id = searchListResult.Snippet.ChannelId;
        channelListRequest.MaxResults = 10;

        Channel? channel = (await channelListRequest.ExecuteAsync()).Items.FirstOrDefault();
        return channel;
    }

    public async Task<Video?> GetVideoAsync(string searchTerm, bool enforceSafeSearch = true, bool getStatistics = false)
    {
        SearchListResponse? searchListResponse = await GetSearchListAsync(searchTerm, enforceSafeSearch);
        if (searchListResponse == null)
        {
            return null;
        }

        SearchResult? searchListResult = searchListResponse.Items.FirstOrDefault(result => result.Id?.Kind == "youtube#video" && !string.IsNullOrEmpty(result.Id?.VideoId));
        if (searchListResult == null)
        {
            return null;
        }
                
        string parts = getStatistics ? "snippet,statistics" : "snippet";
        VideosResource.ListRequest videoListRequest = _youTubeService!.Videos.List(parts);

        videoListRequest.Id = searchListResult.Id.VideoId;
        videoListRequest.MaxResults = 10;

        Video? video = (await videoListRequest.ExecuteAsync()).Items.FirstOrDefault();
        return video;
    }

    public async Task<List<SearchResult>?> SearchAsync(string searchTerm, string? kind = null, bool enforceSafeSearch = true)
    {
        SearchListResponse? searchListResponse = await GetSearchListAsync(searchTerm, enforceSafeSearch);
        if (searchListResponse == null)
        {
            return null;
        }

        var snippets = new List<SearchResult>();
        foreach (SearchResult? item in searchListResponse.Items)
        {
            if (item != null && item.Snippet != null && (string.IsNullOrEmpty(kind) || item.Id.Kind == kind))
            {
                snippets.Add(item);
            }
        }

        return snippets;
    }

    private async Task<SearchListResponse?> GetSearchListAsync(string searchTerm, bool enforceSafeSearch)
    {
        if (_youTubeService == null)
        {
            return null;
        }

        SearchResource.ListRequest searchListRequest = _youTubeService.Search.List("snippet");
        searchListRequest.Q = searchTerm;
        searchListRequest.MaxResults = 10;

        if (enforceSafeSearch)
        {
            searchListRequest.SafeSearch = SearchResource.ListRequest.SafeSearchEnum.Strict;
        }

        SearchListResponse searchListResponse = await searchListRequest.ExecuteAsync();
        return searchListResponse;
    }

    public void Dispose()
    {
        _youTubeService?.Dispose();
        GC.SuppressFinalize(this);
    }
}
