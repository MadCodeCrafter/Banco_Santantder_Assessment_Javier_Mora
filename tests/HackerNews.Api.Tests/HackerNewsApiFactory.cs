using HackerNews.Api.Tests.Fakes;
using HackerNews.Application.Ports;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HackerNews.Api.Tests;

internal sealed class HackerNewsApiFactory : WebApplicationFactory<Program>
{
    public FakeApiGateway Gateway { get; } = new();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHackerNewsGateway>();
            services.AddSingleton<IHackerNewsGateway>(Gateway);
        });
    }
}
