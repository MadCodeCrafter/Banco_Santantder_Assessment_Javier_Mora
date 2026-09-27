using System.Collections.Concurrent;
using HackerNews.Application.Configuration;
using HackerNews.Application.Ports;
using HackerNews.Domain.Stories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HackerNews.Infrastructure.Caching;

public sealed class MemoryStoryCache : IStoryCache
{
    private const string BestStoryIdsKey = "best-story-ids";

    private readonly IMemoryCache _cache;
    private readonly CacheOptions _options;

    private readonly ConcurrentDictionary<string, Lazy<Task<IReadOnlyList<int>>>> _idsInflight = new();
    private readonly ConcurrentDictionary<int, Lazy<Task<Story?>>> _storyInflight = new();

    public MemoryStoryCache(IMemoryCache cache, IOptions<CacheOptions> options)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options?.Value ?? new CacheOptions();
    }

    public async Task<IReadOnlyList<int>> GetOrAddBestStoryIdsAsync(
        Func<CancellationToken, Task<IReadOnlyList<int>>> factory,
        CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(BestStoryIdsKey, out IReadOnlyList<int>? cached) && cached is not null)
        {
            return cached;
        }

        var lazy = _idsInflight.GetOrAdd(
            BestStoryIdsKey,
            _ => new Lazy<Task<IReadOnlyList<int>>>(() => factory(CancellationToken.None)));
        try
        {
            var ids = await lazy.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
            _cache.Set(BestStoryIdsKey, ids, TimeSpan.FromSeconds(_options.BestStoriesIdsTtlSeconds));
            return ids;
        }
        finally
        {
            if (lazy.IsValueCreated && lazy.Value.IsCompleted)
            {
                _idsInflight.TryRemove(new KeyValuePair<string, Lazy<Task<IReadOnlyList<int>>>>(BestStoryIdsKey, lazy));
            }
        }
    }

    public async Task<Story?> GetOrAddStoryAsync(
        int id,
        Func<CancellationToken, Task<Story?>> factory,
        CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(id, out Story? cached) && cached is not null)
        {
            return cached;
        }

        var lazy = _storyInflight.GetOrAdd(
            id,
            _ => new Lazy<Task<Story?>>(() => factory(CancellationToken.None)));
        try
        {
            var story = await lazy.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
            if (story is not null)
            {
                _cache.Set(id, story, TimeSpan.FromSeconds(_options.StoryTtlSeconds));
            }

            return story;
        }
        finally
        {
            if (lazy.IsValueCreated && lazy.Value.IsCompleted)
            {
                _storyInflight.TryRemove(new KeyValuePair<int, Lazy<Task<Story?>>>(id, lazy));
            }
        }
    }
}
