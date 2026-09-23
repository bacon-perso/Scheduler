using Bacon.Scheduler.Interfaces.RateLimiters;
using Bacon.Scheduler.Interfaces.Services.RateLimiters;
using Bacon.Scheduler.Models;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Bacon.Scheduler.Services.RateLimiters;

internal sealed partial class RedisRateLimiterService : IConcurrencyRateLimiterService
{
    #region CTOR

    private readonly RedisDistributedSemaphore _redisDistributedSemaphore;
    private readonly TimeSpan _timeout;
    private readonly ILogger _logger;

    public RedisRateLimiterService(IServiceProvider serviceProvider, IOptions<SchedulerOptions> schedulerOptions, ILogger<RedisRateLimiterService> logger)
    {
        SchedulerOptions _schedulerOptions = schedulerOptions.Value;

        _timeout = _schedulerOptions.ConcurrencyRateLimiter.AcquireTimeout;
        _logger = logger;

        IConnectionMultiplexer connectionMultiplexer = (_schedulerOptions.ConcurrencyRateLimiter.ConnectionMultiplexerFactory?.Invoke(serviceProvider)) ?? throw new InvalidOperationException("The connection multiplexer is required");
        IDatabase db = connectionMultiplexer.GetDatabase();

        _redisDistributedSemaphore = new($"{_schedulerOptions.TenantId}_scheduler_concurrency_distributed_gate", _schedulerOptions.ConcurrencyRateLimiter.PermitLimit, database: db,
            o => o
                .Expiry(_schedulerOptions.ConcurrencyRateLimiter.RedisExpiry)
                .ExtensionCadence(_schedulerOptions.ConcurrencyRateLimiter.RedisExtensionCadence));
    }

    #endregion CTOR

    public async ValueTask<IConcurrencyRateLimitHandle?> TryAcquireAsync(CancellationToken cancellationToken)
    {
        try
        {
            IDistributedSynchronizationHandle? distributedSynchronizationHandle = await _redisDistributedSemaphore.TryAcquireAsync(_timeout, cancellationToken);

            return distributedSynchronizationHandle is null ? null : new RedisRateLimiterHandle(distributedSynchronizationHandle);
        }
        catch (OperationCanceledException oce) when (cancellationToken.IsCancellationRequested)
        {
            LogTryAcquireCancelled(oce);
            throw;
        }
        catch (Exception e)
        {
            LogTryAcquireError(e);
            return null;
        }
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public sealed class RedisRateLimiterHandle(IDistributedSynchronizationHandle inner) : IConcurrencyRateLimitHandle
    {
        public async ValueTask DisposeAsync()
        {
            await inner.DisposeAsync();

            GC.SuppressFinalize(this);
        }
    }

    #region Logs

    [LoggerMessage(Level = LogLevel.Warning, Message = "Scheduler - There was an error acquiring a lease. The request was cancelled")]
    private partial void LogTryAcquireCancelled(OperationCanceledException operationCanceledException);

    [LoggerMessage(Level = LogLevel.Error, Message = "Scheduler - There was an error acquiring a lease")]
    private partial void LogTryAcquireError(Exception exception);

    #endregion Logs
}