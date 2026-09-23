namespace Bacon.Scheduler.Models.Schedules;

/// <summary>
/// Defines the retry configs
/// </summary>
public sealed class ScheduleRetryConfigs
{
    /// <summary>
    /// Defines if the schedule will re-schedule itself or not
    /// </summary>
    public bool AutoRestartOnFailure { get; set; }

    /// <summary>
    /// The schedule's maximum allowed amount of retry
    /// </summary>
    public byte RetryLimit { get; set; }

    /// <summary>
    /// The current schedule's retry count
    /// </summary>
    public byte RetryCount { get; set; }

    /// <summary>
    /// Defines the date and time that will be used for the next try
    /// </summary>
    public DateTime? NextRetryDate { get; set; }

    /// <summary>
    /// The last batch id that was linked to the schedule during a retry. Does not update on Manual execution, on schedule with no retries, nor on delayed execution
    /// </summary>
    public Guid? CurrentRunningBatchId { get; set; }
}