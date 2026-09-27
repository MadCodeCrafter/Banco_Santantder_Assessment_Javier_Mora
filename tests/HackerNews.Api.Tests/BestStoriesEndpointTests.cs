using System.Net;
using System.Net.Http.Json;
using HackerNews.Api.Tests.Fakes;
using HackerNews.Application.BestStories;

namespace HackerNews.Api.Tests;

public sealed class BestStoriesEndpointTests
{
    [Fact]
    public async Task Get_WithValidN_Returns200_AndOrderedByScoreDescending()
    {
        using var factory = new HackerNewsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/best-stories?n=3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stories = await response.Content.ReadFromJsonAsync<List<BestStoryDto>>();
        Assert.NotNull(stories);
        Assert.Equal(3, stories!.Count);

        var scores = stories.Select(s => s.Score).ToArray();
        var sorted = scores.OrderByDescending(x => x).ToArray();
        Assert.Equal(sorted, scores);
    }

    [Fact]
    public async Task Get_WithValidN_ReturnsExpectedContractProperties()
    {
        using var factory = new HackerNewsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/best-stories?n=1");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"title\"", json);
        Assert.Contains("\"uri\"", json);
        Assert.Contains("\"postedBy\"", json);
        Assert.Contains("\"time\"", json);
        Assert.Contains("\"score\"", json);
        Assert.Contains("\"commentCount\"", json);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task Get_WithInvalidN_Returns400(int n)
    {
        using var factory = new HackerNewsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/best-stories?n={n}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_WhenUpstreamUnavailable_Returns502()
    {
        using var factory = new HackerNewsApiFactory();
        factory.Gateway.Mode = FakeApiGateway.GatewayMode.UpstreamUnavailable;
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/best-stories?n=3");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task Get_WhenUpstreamTimeout_Returns504()
    {
        using var factory = new HackerNewsApiFactory();
        factory.Gateway.Mode = FakeApiGateway.GatewayMode.UpstreamTimeout;
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/best-stories?n=3");

        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerJson_IsAvailable()
    {
        using var factory = new HackerNewsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
