using Bacon.Scheduler.Interfaces.RateLimiters;
using Bacon.Scheduler.Interfaces.Services.RateLimiters;

namespace Bacon.Scheduler.Tests.Fakes;

internal sealed class FakeConcurrencyRateLimiterService : IConcurrencyRateLimiterService
{
    public bool GrantLease { get; set; } = true;
    public int AcquireCallCount { get; private set; }

    public FakeConcurrencyRateLimitHandle? LastHandle { get; private set; }

    public ValueTask<IConcurrencyRateLimitHandle?> TryAcquireAsync(CancellationToken cancellationToken)
    {
        AcquireCallCount++;

        if (!GrantLease)
        {
            return ValueTask.FromResult<IConcurrencyRateLimitHandle?>(null);
        }

        LastHandle = new FakeConcurrencyRateLimitHandle();
        return ValueTask.FromResult<IConcurrencyRateLimitHandle?>(LastHandle);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class FakeConcurrencyRateLimitHandle : IConcurrencyRateLimitHandle
{
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}
