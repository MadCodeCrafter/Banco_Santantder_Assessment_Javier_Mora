using HackerNews.Application.BestStories;
using Microsoft.Extensions.DependencyInjection;

namespace HackerNews.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IGetBestStoriesQueryHandler, GetBestStoriesQueryHandler>();
        return services;
    }
}
