using HackerNews.Application.Ports;
using HackerNews.Domain.Stories;

namespace HackerNews.Application.Tests.Fakes;

internal sealed class PassthroughStoryCache : IStoryCache
{
    public Task<IReadOnlyList<int>> GetOrAddBestStoryIdsAsync(
        Func<CancellationToken, Task<IReadOnlyList<int>>> factory,
        CancellationToken cancellationToken) => factory(cancellationToken);

    public Task<Story?> GetOrAddStoryAsync(
        int id,
        Func<CancellationToken, Task<Story?>> factory,
        CancellationToken cancellationToken) => factory(cancellationToken);
}
