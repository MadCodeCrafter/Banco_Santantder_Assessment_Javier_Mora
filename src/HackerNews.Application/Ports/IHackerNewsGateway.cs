using HackerNews.Domain.Stories;

namespace HackerNews.Application.Ports;

public interface IHackerNewsGateway
{
    Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken);

    Task<Story?> GetItemAsync(int id, CancellationToken cancellationToken);
}
