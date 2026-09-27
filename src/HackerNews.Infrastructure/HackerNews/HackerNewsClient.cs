using System.Net.Http.Json;
using System.Text.Json;
using HackerNews.Application.BestStories;
using HackerNews.Application.Ports;
using HackerNews.Domain.Stories;
using Microsoft.Extensions.Logging;

namespace HackerNews.Infrastructure.HackerNews;

public sealed class HackerNewsClient : IHackerNewsGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<HackerNewsClient> _logger;

    public HackerNewsClient(HttpClient httpClient, ILogger<HackerNewsClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var ids = await _httpClient
                .GetFromJsonAsync<int[]>("beststories.json", JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            return ids ?? Array.Empty<int>();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Timeout while fetching best story ids from Hacker News.");
            throw new UpstreamTimeoutException("Timeout while fetching best story ids from Hacker News.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching best story ids from Hacker News.");
            throw new UpstreamUnavailableException("Failed to fetch best story ids from Hacker News.", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON while fetching best story ids from Hacker News.");
            throw new UpstreamUnavailableException("Invalid response while fetching best story ids from Hacker News.", ex);
        }
    }

    public async Task<Story?> GetItemAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _httpClient
                .GetFromJsonAsync<HackerNewsItemResponse>($"item/{id}.json", JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (item is null)
            {
                return null;
            }

            return new Story
            {
                Id = item.Id,
                Title = item.Title ?? string.Empty,
                Uri = item.Url,
                PostedBy = item.By ?? string.Empty,
                Time = item.Time.HasValue
                    ? DateTimeOffset.FromUnixTimeSeconds(item.Time.Value)
                    : DateTimeOffset.MinValue,
                Score = item.Score ?? 0,
                CommentCount = item.Descendants ?? 0
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to fetch item {Id} from Hacker News; skipping.", id);
            return null;
        }
    }
}
