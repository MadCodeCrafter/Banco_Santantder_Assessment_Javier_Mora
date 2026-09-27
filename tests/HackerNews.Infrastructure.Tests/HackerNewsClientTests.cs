using System.Net;
using HackerNews.Application.BestStories;
using HackerNews.Infrastructure.HackerNews;
using HackerNews.Infrastructure.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace HackerNews.Infrastructure.Tests;

public sealed class HackerNewsClientTests
{
    private static HackerNewsClient CreateClient(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/")
        };
        return new HackerNewsClient(httpClient, NullLogger<HackerNewsClient>.Instance);
    }

    [Fact]
    public async Task GetBestStoryIdsAsync_ReturnsIds()
    {
        var handler = StubHttpMessageHandler.Json(_ => (HttpStatusCode.OK, "[1,2,3,4]"));
        var client = CreateClient(handler);

        var ids = await client.GetBestStoryIdsAsync(CancellationToken.None);

        Assert.Equal(new[] { 1, 2, 3, 4 }, ids);
    }

    [Fact]
    public async Task GetBestStoryIdsAsync_WhenServerError_ThrowsUpstreamUnavailable()
    {
        var handler = StubHttpMessageHandler.Json(_ => (HttpStatusCode.InternalServerError, ""));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<UpstreamUnavailableException>(
            () => client.GetBestStoryIdsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetItemAsync_MapsAllFields_AndConvertsUnixTime()
    {
        const string json = """
        {
          "id": 8863,
          "title": "My Story",
          "url": "https://example.com/story",
          "by": "dhouston",
          "time": 1175714200,
          "score": 111,
          "descendants": 71,
          "type": "story"
        }
        """;
        var handler = StubHttpMessageHandler.Json(_ => (HttpStatusCode.OK, json));
        var client = CreateClient(handler);

        var story = await client.GetItemAsync(8863, CancellationToken.None);

        Assert.NotNull(story);
        Assert.Equal(8863, story!.Id);
        Assert.Equal("My Story", story.Title);
        Assert.Equal("https://example.com/story", story.Uri);
        Assert.Equal("dhouston", story.PostedBy);
        Assert.Equal(111, story.Score);
        Assert.Equal(71, story.CommentCount);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1175714200), story.Time);
    }

    [Fact]
    public async Task GetItemAsync_WhenUrlMissing_MapsNullUri()
    {
        const string json = """
        { "id": 1, "title": "Ask HN", "by": "someone", "time": 1175714200, "score": 5 }
        """;
        var handler = StubHttpMessageHandler.Json(_ => (HttpStatusCode.OK, json));
        var client = CreateClient(handler);

        var story = await client.GetItemAsync(1, CancellationToken.None);

        Assert.NotNull(story);
        Assert.Null(story!.Uri);
    }

    [Fact]
    public async Task GetItemAsync_WhenDescendantsMissing_MapsZeroComments()
    {
        const string json = """
        { "id": 1, "title": "No comments field", "by": "someone", "time": 1175714200, "score": 5 }
        """;
        var handler = StubHttpMessageHandler.Json(_ => (HttpStatusCode.OK, json));
        var client = CreateClient(handler);

        var story = await client.GetItemAsync(1, CancellationToken.None);

        Assert.NotNull(story);
        Assert.Equal(0, story!.CommentCount);
    }

    [Fact]
    public async Task GetItemAsync_WhenNullResponse_ReturnsNull()
    {
        var handler = StubHttpMessageHandler.Json(_ => (HttpStatusCode.OK, "null"));
        var client = CreateClient(handler);

        var story = await client.GetItemAsync(999, CancellationToken.None);

        Assert.Null(story);
    }

    [Fact]
    public async Task GetItemAsync_WhenNotFound_ReturnsNull_WithoutThrowing()
    {
        var handler = StubHttpMessageHandler.Json(_ => (HttpStatusCode.NotFound, ""));
        var client = CreateClient(handler);

        var story = await client.GetItemAsync(999, CancellationToken.None);

        Assert.Null(story);
    }

    [Fact]
    public async Task GetItemAsync_WhenInvalidJson_ReturnsNull_WithoutThrowing()
    {
        var handler = StubHttpMessageHandler.Json(_ => (HttpStatusCode.OK, "{ this is not valid json"));
        var client = CreateClient(handler);

        var story = await client.GetItemAsync(999, CancellationToken.None);

        Assert.Null(story);
    }

    [Fact]
    public async Task GetBestStoryIdsAsync_WhenCancelled_ThrowsOperationCanceled()
    {
        var handler = StubHttpMessageHandler.Json(_ => (HttpStatusCode.OK, "[1,2,3]"));
        var client = CreateClient(handler);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetBestStoryIdsAsync(cts.Token));
    }
}
