using StackExchange.Redis;

namespace Bacon.Scheduler.Models;

/// <summary>
/// Defines the concurrency rate limiter options
/// </summary>
public sealed class ConcurrencyRateLimiterOptions
{
    /// <summary>
    /// Defines how the concurrency limiter will be handled
    /// </summary>
    public ConcurrencyRateLimitProviders ConcurrencyRateLimitProvider { get; set; } = ConcurrencyRateLimitProviders.Local;

    /// <summary>
    /// Defines the number of concurrent operation
    /// </summary>
    internal int PermitLimit { get; } = 1;

    /// <summary>
    /// How long a caller will wait to acquire before TryAcquireAsync returns null. In Local mode this caps queue wait time; in Redis mode this is the only backpressure mechanism (Redis has no bounded queue).
    /// </summary>
    internal TimeSpan AcquireTimeout { get; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Local mode only. Max waiters before new callers are rejected immediately.
    /// Ignored in Redis mode — use <see cref="AcquireTimeout"/> for backpressure instead.
    /// </summary>
    internal int LocalQueueLimit { get; }

    /// <summary>
    /// Redis-only. Allows to configure the connection multiplexer from scratch or use an existing one
    /// </summary>
    public Func<IServiceProvider, IConnectionMultiplexer>? ConnectionMultiplexerFactory { get; set; }

    /// <summary>
    /// Redis-only. Expiry is "max time after a crash before reclaim"
    /// ExtensionCadence is how often the background task renews the lease.
    /// </summary>
    internal TimeSpan RedisExpiry { get; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Defines the heartbeat check done. Usually 1/3 of the RedisExpiry
    /// </summary>
    internal TimeSpan RedisExtensionCadence { get; } = TimeSpan.FromSeconds(10);
}