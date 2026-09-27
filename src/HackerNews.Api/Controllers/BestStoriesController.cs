using HackerNews.Application.BestStories;
using HackerNews.Application.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HackerNews.Api.Controllers;

[ApiController]
[Route("api/best-stories")]
[Produces("application/json")]
public sealed class BestStoriesController : ControllerBase
{
    private readonly IGetBestStoriesQueryHandler _handler;
    private readonly BestStoriesOptions _options;

    public BestStoriesController(IGetBestStoriesQueryHandler handler, IOptions<BestStoriesOptions> options)
    {
        _handler = handler;
        _options = options?.Value ?? new BestStoriesOptions();
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BestStoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<IReadOnlyList<BestStoryDto>>> GetBestStories(
        [FromQuery] int n,
        CancellationToken cancellationToken)
    {
        if (n <= 0)
        {
            return Problem(
                title: "Invalid request",
                detail: "The query parameter 'n' must be greater than zero.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (n > _options.MaxCount)
        {
            return Problem(
                title: "Invalid request",
                detail: $"The query parameter 'n' must be less than or equal to {_options.MaxCount}.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var stories = await _handler.HandleAsync(new GetBestStoriesQuery(n), cancellationToken).ConfigureAwait(false);
        return Ok(stories);
    }
}
