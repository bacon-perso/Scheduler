using Bacon.Scheduler.Interfaces.RateLimiters;
using Bacon.Scheduler.Interfaces.Services.RateLimiters;
using Bacon.Scheduler.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

namespace Bacon.Scheduler.Services.RateLimiters;

internal sealed partial class LocalRateLimiterService : IConcurrencyRateLimiterService
{
    #region CTOR

    private readonly ConcurrencyRateLimiterOptions _concurrencyRateLimiterOptions;
    private readonly TimeSpan _timeout;
    private readonly ConcurrencyLimiter _concurrencyLimiter;
    private readonly ILogger _logger;

    public LocalRateLimiterService(IOptions<ConcurrencyRateLimiterOptions> concurrencyRateLimiterOptions, ILogger<LocalRateLimiterService> logger)
    {
        _concurrencyRateLimiterOptions = concurrencyRateLimiterOptions.Value;
        _timeout = _concurrencyRateLimiterOptions.AcquireTimeout;
        _logger = logger;

        _concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
        {
            PermitLimit = _concurrencyRateLimiterOptions.PermitLimit,
            QueueLimit = _concurrencyRateLimiterOptions.LocalQueueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        });
    }

    #endregion CTOR

    public async ValueTask<IConcurrencyRateLimitHandle?> TryAcquireAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationTokenSource.CancelAfter(_timeout);

        try
        {
            RateLimitLease rateLimitLease = await _concurrencyLimiter.AcquireAsync(permitCount: 1, cancellationTokenSource.Token);

            return rateLimitLease.IsAcquired ? new LocalRateLimiterHandle(rateLimitLease) : null;
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
        _concurrencyLimiter.Dispose();
        return ValueTask.CompletedTask;
    }

    public sealed class LocalRateLimiterHandle(RateLimitLease rateLimitLease) : IConcurrencyRateLimitHandle
    {
        public ValueTask DisposeAsync()
        {
            // RateLimitLease is IDisposable, not IAsyncDisposable
            rateLimitLease.Dispose();  
            GC.SuppressFinalize(this);

            return ValueTask.CompletedTask;
        }
    }

    #region Logs

    [LoggerMessage(Level = LogLevel.Warning, Message = "Scheduler - There was an error acquiring a lease. The request was cancelled")]
    private partial void LogTryAcquireCancelled(OperationCanceledException operationCanceledException);

    [LoggerMessage(Level = LogLevel.Error, Message = "Scheduler - There was an error acquiring a lease")]
    private partial void LogTryAcquireError(Exception exception);

    #endregion Logs
}