namespace HackerNews.Application.BestStories;

public sealed class BestStoryDto
{
    public string Title { get; init; } = string.Empty;
    public string? Uri { get; init; }
    public string PostedBy { get; init; } = string.Empty;
    public DateTimeOffset Time { get; init; }
    public int Score { get; init; }
    public int CommentCount { get; init; }
}
