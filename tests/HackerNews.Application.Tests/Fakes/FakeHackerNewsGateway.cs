using HackerNews.Application.Ports;
using HackerNews.Domain.Stories;

namespace HackerNews.Application.Tests.Fakes;

internal sealed class FakeHackerNewsGateway : IHackerNewsGateway
{
    private readonly IReadOnlyList<int> _ids;
    private readonly IReadOnlyDictionary<int, Story?> _stories;

    public int BestStoryIdsCallCount { get; private set; }
    private readonly Dictionary<int, int> _itemCallCounts = new();

    public FakeHackerNewsGateway(IReadOnlyList<int> ids, IReadOnlyDictionary<int, Story?> stories)
    {
        _ids = ids;
        _stories = stories;
    }

    public int ItemCallCount(int id) => _itemCallCounts.TryGetValue(id, out var c) ? c : 0;
    public int TotalItemCallCount => _itemCallCounts.Values.Sum();

    public Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        BestStoryIdsCallCount++;
        return Task.FromResult(_ids);
    }

    public Task<Story?> GetItemAsync(int id, CancellationToken cancellationToken)
    {
        _itemCallCounts[id] = ItemCallCount(id) + 1;
        _stories.TryGetValue(id, out var story);
        return Task.FromResult(story);
    }
}
