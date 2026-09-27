namespace HackerNews.Application.Configuration;

public sealed class HackerNewsOptions
{
    public const string SectionName = "HackerNews";

    public string BaseUrl { get; set; } = "https://hacker-news.firebaseio.com/v0/";

    public int TimeoutSeconds { get; set; } = 10;

    public int MaxConcurrency { get; set; } = 8;
}

public sealed class BestStoriesOptions
{
    public const string SectionName = "BestStories";

    public int MaxCount { get; set; } = 100;
}

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public int BestStoriesIdsTtlSeconds { get; set; } = 30;
    public int StoryTtlSeconds { get; set; } = 60;
}
