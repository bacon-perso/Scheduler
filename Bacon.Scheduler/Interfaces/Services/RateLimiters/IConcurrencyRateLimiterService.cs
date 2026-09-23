using Bacon.Scheduler.Interfaces.RateLimiters;

namespace Bacon.Scheduler.Interfaces.Services.RateLimiters;

internal interface IConcurrencyRateLimiterService : IAsyncDisposable
{
    ValueTask<IConcurrencyRateLimitHandle?> TryAcquireAsync(CancellationToken cancellationToken);
}