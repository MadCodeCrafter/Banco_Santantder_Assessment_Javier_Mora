using HackerNews.Application.Ports;
using HackerNews.Domain.Stories;

namespace HackerNews.Api.Tests.Fakes;

internal sealed class FakeApiGateway : IHackerNewsGateway
{
    public enum GatewayMode
    {
        Success,
        UpstreamUnavailable,
        UpstreamTimeout
    }

    public GatewayMode Mode { get; set; } = GatewayMode.Success;

    public Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        return Mode switch
        {
            GatewayMode.UpstreamUnavailable =>
                throw new Application.BestStories.UpstreamUnavailableException("boom"),
            GatewayMode.UpstreamTimeout =>
                throw new Application.BestStories.UpstreamTimeoutException("slow"),
            _ => Task.FromResult<IReadOnlyList<int>>(new[] { 1, 2, 3 })
        };
    }

    public Task<Story?> GetItemAsync(int id, CancellationToken cancellationToken)
    {
        var story = new Story
        {
            Id = id,
            Title = $"Story {id}",
            Uri = $"https://example.com/{id}",
            PostedBy = "author",
            Time = DateTimeOffset.FromUnixTimeSeconds(1175714200),
            Score = id * 10,
            CommentCount = id
        };
        return Task.FromResult<Story?>(story);
    }
}
