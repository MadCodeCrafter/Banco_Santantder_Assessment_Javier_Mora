namespace HackerNews.Application.BestStories;

public interface IGetBestStoriesQueryHandler
{
    Task<IReadOnlyList<BestStoryDto>> HandleAsync(GetBestStoriesQuery query, CancellationToken cancellationToken);
}
