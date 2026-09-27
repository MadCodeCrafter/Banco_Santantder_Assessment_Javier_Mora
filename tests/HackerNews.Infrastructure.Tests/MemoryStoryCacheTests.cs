using HackerNews.Application.Configuration;
using HackerNews.Domain.Stories;
using HackerNews.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HackerNews.Infrastructure.Tests;

public sealed class MemoryStoryCacheTests
{
    private static MemoryStoryCache CreateCache()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new CacheOptions
        {
            BestStoriesIdsTtlSeconds = 30,
            StoryTtlSeconds = 60
        });
        return new MemoryStoryCache(memoryCache, options);
    }

    private static Story StoryWith(int id) => new()
    {
        Id = id,
        Title = $"Story {id}",
        PostedBy = "author",
        Time = DateTimeOffset.UnixEpoch,
        Score = id,
        CommentCount = 0
    };

    [Fact]
    public async Task GetOrAddStoryAsync_CachesResult_AndAvoidsSecondFactoryCall()
    {
        var cache = CreateCache();
        var callCount = 0;

        Task<Story?> Factory(CancellationToken _)
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult<Story?>(StoryWith(1));
        }

        var first = await cache.GetOrAddStoryAsync(1, Factory, CancellationToken.None);
        var second = await cache.GetOrAddStoryAsync(1, Factory, CancellationToken.None);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task GetOrAddStoryAsync_WhenFactoryReturnsNull_DoesNotCache()
    {
        var cache = CreateCache();
        var callCount = 0;

        Task<Story?> Factory(CancellationToken _)
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult<Story?>(null);
        }

        await cache.GetOrAddStoryAsync(5, Factory, CancellationToken.None);
        await cache.GetOrAddStoryAsync(5, Factory, CancellationToken.None);

        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task GetOrAddStoryAsync_ConcurrentCallers_ShareSingleFactoryInvocation()
    {
        var cache = CreateCache();
        var callCount = 0;
        var gate = new TaskCompletionSource();

        async Task<Story?> SlowFactory(CancellationToken _)
        {
            Interlocked.Increment(ref callCount);
            await gate.Task;
            return StoryWith(7);
        }

        var tasks = Enumerable.Range(0, 50)
            .Select(_ => cache.GetOrAddStoryAsync(7, SlowFactory, CancellationToken.None))
            .ToArray();

        gate.SetResult();
        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.NotNull(r));
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task GetOrAddBestStoryIdsAsync_CachesResult()
    {
        var cache = CreateCache();
        var callCount = 0;

        Task<IReadOnlyList<int>> Factory(CancellationToken _)
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult<IReadOnlyList<int>>(new[] { 1, 2, 3 });
        }

        var first = await cache.GetOrAddBestStoryIdsAsync(Factory, CancellationToken.None);
        var second = await cache.GetOrAddBestStoryIdsAsync(Factory, CancellationToken.None);

        Assert.Equal(new[] { 1, 2, 3 }, first);
        Assert.Equal(new[] { 1, 2, 3 }, second);
        Assert.Equal(1, callCount);
    }
}
