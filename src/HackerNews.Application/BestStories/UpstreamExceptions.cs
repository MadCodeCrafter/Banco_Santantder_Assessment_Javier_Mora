namespace HackerNews.Application.BestStories;

public sealed class UpstreamUnavailableException : Exception
{
    public UpstreamUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class UpstreamTimeoutException : Exception
{
    public UpstreamTimeoutException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
