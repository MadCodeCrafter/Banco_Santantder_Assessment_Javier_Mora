using HackerNews.Application.Configuration;
using HackerNews.Application.Ports;
using Microsoft.Extensions.Options;

namespace HackerNews.Infrastructure.Concurrency;

public sealed class SemaphoreUpstreamConcurrencyLimiter : IUpstreamConcurrencyLimiter, IDisposable
{
    private readonly SemaphoreSlim _semaphore;

    public SemaphoreUpstreamConcurrencyLimiter(IOptions<HackerNewsOptions> options)
    {
        var max = Math.Max(1, options?.Value?.MaxConcurrency ?? 8);
        _semaphore = new SemaphoreSlim(max, max);
    }

    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Lease(_semaphore);
    }

    public void Dispose() => _semaphore.Dispose();

    private sealed class Lease : IDisposable
    {
        private SemaphoreSlim? _semaphore;

        public Lease(SemaphoreSlim semaphore) => _semaphore = semaphore;

        public void Dispose()
        {
            Interlocked.Exchange(ref _semaphore, null)?.Release();
        }
    }
}
