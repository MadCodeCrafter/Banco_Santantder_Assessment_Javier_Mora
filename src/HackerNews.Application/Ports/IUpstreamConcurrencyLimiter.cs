namespace HackerNews.Application.Ports;

public interface IUpstreamConcurrencyLimiter
{
    Task<IDisposable> AcquireAsync(CancellationToken cancellationToken);
}
