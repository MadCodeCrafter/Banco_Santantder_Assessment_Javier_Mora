using HackerNews.Application.BestStories;
using HackerNews.Application.Configuration;
using HackerNews.Application.Ports;
using HackerNews.Application.Tests.Fakes;
using HackerNews.Domain.Stories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HackerNews.Application.Tests;

public sealed class GetBestStoriesQueryHandlerTests
{
    private static Story StoryWith(int id, int score) => new()
    {
        Id = id,
        Title = $"Story {id}",
        Uri = $"https://example.com/{id}",
        PostedBy = "author",
        Time = DateTimeOffset.FromUnixTimeSeconds(1_570_887_781),
        Score = score,
        CommentCount = id
    };

    private static GetBestStoriesQueryHandler CreateHandler(
        IHackerNewsGateway gateway,
        IStoryCache? cache = null,
        int maxCount = 100,
        int maxConcurrency = 8)
    {
        var bestOptions = Options.Create(new BestStoriesOptions { MaxCount = maxCount });
        return new GetBestStoriesQueryHandler(
            gateway,
            cache ?? new PassthroughStoryCache(),
            new TestConcurrencyLimiter(maxConcurrency),
            bestOptions,
            NullLogger<GetBestStoriesQueryHandler>.Instance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task HandleAsync_WhenCountIsNotPositive_Throws(int count)
    {
        var gateway = new FakeHackerNewsGateway([], new Dictionary<int, Story?>());
        var handler = CreateHandler(gateway);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => handler.HandleAsync(new GetBestStoriesQuery(count), CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_WhenCountExceedsMax_Throws()
    {
        var gateway = new FakeHackerNewsGateway([], new Dictionary<int, Story?>());
        var handler = CreateHandler(gateway, maxCount: 50);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => handler.HandleAsync(new GetBestStoriesQuery(51), CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_ReturnsExactlyNStories_WhenEnoughExist()
    {
        var ids = new[] { 1, 2, 3, 4, 5 };
        var stories = ids.ToDictionary(id => id, id => (Story?)StoryWith(id, score: id * 10));
        var gateway = new FakeHackerNewsGateway(ids, stories);
        var handler = CreateHandler(gateway);

        var result = await handler.HandleAsync(new GetBestStoriesQuery(3), CancellationToken.None);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task HandleAsync_OrdersByScoreDescending()
    {
        var ids = new[] { 1, 2, 3, 4 };
        var stories = new Dictionary<int, Story?>
        {
            [1] = StoryWith(1, score: 50),
            [2] = StoryWith(2, score: 200),
            [3] = StoryWith(3, score: 10),
            [4] = StoryWith(4, score: 120)
        };
        var gateway = new FakeHackerNewsGateway(ids, stories);
        var handler = CreateHandler(gateway);

        var result = await handler.HandleAsync(new GetBestStoriesQuery(4), CancellationToken.None);

        var scores = result.Select(s => s.Score).ToArray();
        Assert.Equal(new[] { 200, 120, 50, 10 }, scores);
    }

    [Fact]
    public async Task HandleAsync_MapsAllFields()
    {
        var story = StoryWith(42, score: 999);
        var gateway = new FakeHackerNewsGateway([42], new Dictionary<int, Story?> { [42] = story });
        var handler = CreateHandler(gateway);

        var result = await handler.HandleAsync(new GetBestStoriesQuery(1), CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(story.Title, dto.Title);
        Assert.Equal(story.Uri, dto.Uri);
        Assert.Equal(story.PostedBy, dto.PostedBy);
        Assert.Equal(story.Time, dto.Time);
        Assert.Equal(story.Score, dto.Score);
        Assert.Equal(story.CommentCount, dto.CommentCount);
    }

    [Fact]
    public async Task HandleAsync_SkipsNullItems_AndTopsUpToN()
    {
        var ids = new[] { 1, 2, 3, 4, 5, 6 };
        var stories = new Dictionary<int, Story?>
        {
            [1] = StoryWith(1, 10),
            [2] = null,
            [3] = StoryWith(3, 30),
            [4] = null,
            [5] = StoryWith(5, 50),
            [6] = StoryWith(6, 60)
        };
        var gateway = new FakeHackerNewsGateway(ids, stories);
        var handler = CreateHandler(gateway);

        var result = await handler.HandleAsync(new GetBestStoriesQuery(3), CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.DoesNotContain(result, r => r.Title is "Story 2" or "Story 4");
    }

    [Fact]
    public async Task HandleAsync_ReturnsFewerThanN_WhenNotEnoughValidStories()
    {
        var ids = new[] { 1, 2 };
        var stories = new Dictionary<int, Story?>
        {
            [1] = StoryWith(1, 10),
            [2] = null
        };
        var gateway = new FakeHackerNewsGateway(ids, stories);
        var handler = CreateHandler(gateway);

        var result = await handler.HandleAsync(new GetBestStoriesQuery(5), CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task HandleAsync_WhenNoIds_ReturnsEmpty()
    {
        var gateway = new FakeHackerNewsGateway([], new Dictionary<int, Story?>());
        var handler = CreateHandler(gateway);

        var result = await handler.HandleAsync(new GetBestStoriesQuery(10), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_WhenCancelled_ThrowsOperationCanceled()
    {
        var ids = new[] { 1, 2, 3 };
        var stories = ids.ToDictionary(id => id, id => (Story?)StoryWith(id, id));
        var gateway = new FakeHackerNewsGateway(ids, stories);
        var handler = CreateHandler(gateway);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetBestStoriesQuery(3), cts.Token));
    }

    [Fact]
    public async Task HandleAsync_ReturnsGlobalTopByScore_WhenHighestScoresAreNotFirst()
    {
        var ids = new[] { 1, 2, 3, 4, 5 };
        var stories = new Dictionary<int, Story?>
        {
            [1] = StoryWith(1, 100),
            [2] = StoryWith(2, 90),
            [3] = StoryWith(3, 80),
            [4] = StoryWith(4, 500),
            [5] = StoryWith(5, 400)
        };
        var gateway = new FakeHackerNewsGateway(ids, stories);
        var handler = CreateHandler(gateway);

        var result = await handler.HandleAsync(new GetBestStoriesQuery(2), CancellationToken.None);

        var scores = result.Select(s => s.Score).ToArray();
        Assert.Equal(new[] { 500, 400 }, scores);
    }

    [Fact]
    public async Task HandleAsync_EvaluatesAllCandidates_ToGuaranteeGlobalTopByScore()
    {
        var ids = Enumerable.Range(1, 10).ToArray();
        var stories = ids.ToDictionary(id => id, id => (Story?)StoryWith(id, score: id * 10));
        var gateway = new FakeHackerNewsGateway(ids, stories);
        var handler = CreateHandler(gateway);

        var result = await handler.HandleAsync(new GetBestStoriesQuery(3), CancellationToken.None);

        Assert.Equal(new[] { 100, 90, 80 }, result.Select(s => s.Score).ToArray());
        Assert.Equal(ids.Length, gateway.TotalItemCallCount);
    }
}
