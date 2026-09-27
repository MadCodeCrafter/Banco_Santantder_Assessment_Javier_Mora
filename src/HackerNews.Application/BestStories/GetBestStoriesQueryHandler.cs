using HackerNews.Application.Configuration;
using HackerNews.Application.Ports;
using HackerNews.Domain.Stories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HackerNews.Application.BestStories;

public sealed class GetBestStoriesQueryHandler : IGetBestStoriesQueryHandler
{
    private readonly IHackerNewsGateway _gateway;
    private readonly IStoryCache _cache;
    private readonly IUpstreamConcurrencyLimiter _concurrencyLimiter;
    private readonly BestStoriesOptions _bestStories;
    private readonly ILogger<GetBestStoriesQueryHandler> _logger;

    public GetBestStoriesQueryHandler(
        IHackerNewsGateway gateway,
        IStoryCache cache,
        IUpstreamConcurrencyLimiter concurrencyLimiter,
        IOptions<BestStoriesOptions> bestStories,
        ILogger<GetBestStoriesQueryHandler> logger)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _concurrencyLimiter = concurrencyLimiter ?? throw new ArgumentNullException(nameof(concurrencyLimiter));
        _bestStories = bestStories?.Value ?? new BestStoriesOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<BestStoryDto>> HandleAsync(GetBestStoriesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(query), query.Count, "The number of stories requested must be greater than zero.");
        }

        if (query.Count > _bestStories.MaxCount)
        {
            throw new ArgumentOutOfRangeException(nameof(query), query.Count, $"The number of stories requested must be less than or equal to {_bestStories.MaxCount}.");
        }

        var ids = await _cache
            .GetOrAddBestStoryIdsAsync(_gateway.GetBestStoryIdsAsync, cancellationToken)
            .ConfigureAwait(false);

        if (ids.Count == 0)
        {
            return Array.Empty<BestStoryDto>();
        }

        //No podemos asumir que los primeros IDs de /beststories sean los de mayor score:
        //ese listado mezcla score, antigüedad y otros factores, no es un ranking estricto.
        //
        //Ejemplo: A=300, B=250, C=200, D=900, E=800. Para n=3, quedarnos con los tres
        //primeros IDs devolvería 300, 250 y 200, dejando fuera 900 y 800.
        //
        //Por eso recuperamos el detalle de todos los candidatos y seleccionamos el top-n
        //real con OrderByDescending(score).Take(n).
        var stories = await FetchStoriesAsync(ids, cancellationToken).ConfigureAwait(false);

        return stories
            .OrderByDescending(s => s.Score)
            .Take(query.Count)
            .Select(Map)
            .ToArray();
    }

    private async Task<IReadOnlyList<Story>> FetchStoriesAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken)
    {
        var tasks = ids.Select(async id =>
        {
            using var lease = await _concurrencyLimiter.AcquireAsync(cancellationToken).ConfigureAwait(false);
            return await _cache
                .GetOrAddStoryAsync(id, ct => _gateway.GetItemAsync(id, ct), cancellationToken)
                .ConfigureAwait(false);
        });

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.Where(s => s is not null)!.Cast<Story>().ToArray();
    }

    private static BestStoryDto Map(Story s) => new()
    {
        Title = s.Title,
        Uri = s.Uri,
        PostedBy = s.PostedBy,
        Time = s.Time,
        Score = s.Score,
        CommentCount = s.CommentCount
    };
}
