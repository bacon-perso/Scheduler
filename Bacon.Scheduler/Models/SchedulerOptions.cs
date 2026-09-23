namespace Bacon.Scheduler.Models;

/// <summary>
/// Defines the scheduler options
/// </summary>
public sealed class SchedulerOptions
{
    /// <summary>
    /// The tenant ID. Used to generate a tenant unique key
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Defines the time interval at which the scheduler will do a long polling in the storage to fetch all the schedules that are ready to be executed, or retried.
    /// Supported accepted range is between 5 seconds and 1 hours. Default value is 1 minute.
    /// </summary>
    public TimeSpan PeriodicTimerInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// This configuration allows the scheduler to have system schedule manually executed. 
    /// </summary>
    public bool AllowSystemScheduleManualExecution { get; set; }

    /// <summary>
    /// Defines the concurrency rate limiter options
    /// </summary>
    public ConcurrencyRateLimiterOptions ConcurrencyRateLimiter { get; set; } = new();
}