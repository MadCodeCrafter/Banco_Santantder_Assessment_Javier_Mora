using HackerNews.Domain.Stories;

namespace HackerNews.Application.Ports;

public interface IStoryCache
{
    Task<IReadOnlyList<int>> GetOrAddBestStoryIdsAsync(
        Func<CancellationToken, Task<IReadOnlyList<int>>> factory,
        CancellationToken cancellationToken);

    Task<Story?> GetOrAddStoryAsync(
        int id,
        Func<CancellationToken, Task<Story?>> factory,
        CancellationToken cancellationToken);
}
