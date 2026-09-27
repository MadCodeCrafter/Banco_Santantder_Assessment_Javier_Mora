using HackerNews.Application.Configuration;
using HackerNews.Application.Ports;
using HackerNews.Infrastructure.Caching;
using HackerNews.Infrastructure.Concurrency;
using HackerNews.Infrastructure.HackerNews;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HackerNews.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HackerNewsOptions>(configuration.GetSection(HackerNewsOptions.SectionName));
        services.Configure<BestStoriesOptions>(configuration.GetSection(BestStoriesOptions.SectionName));
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        services.AddMemoryCache();
        services.AddSingleton<IStoryCache, MemoryStoryCache>();

        services.AddSingleton<IUpstreamConcurrencyLimiter, SemaphoreUpstreamConcurrencyLimiter>();

        services.AddHttpClient<IHackerNewsGateway, HackerNewsClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<HackerNewsOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        return services;
    }
}
