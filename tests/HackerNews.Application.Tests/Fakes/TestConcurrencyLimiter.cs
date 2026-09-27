using HackerNews.Application.Ports;

namespace HackerNews.Application.Tests.Fakes;

public sealed class TestConcurrencyLimiter : IUpstreamConcurrencyLimiter
{
    private readonly SemaphoreSlim _semaphore;

    public TestConcurrencyLimiter(int maxConcurrency)
    {
        var max = Math.Max(1, maxConcurrency);
        _semaphore = new SemaphoreSlim(max, max);
    }

    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Lease(_semaphore);
    }

    private sealed class Lease(SemaphoreSlim semaphore) : IDisposable
    {
        private SemaphoreSlim? _semaphore = semaphore;

        public void Dispose() => Interlocked.Exchange(ref _semaphore, null)?.Release();
    }
}
