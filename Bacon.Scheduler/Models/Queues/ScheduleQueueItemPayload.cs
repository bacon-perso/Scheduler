using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Models.Schedules;

namespace Bacon.Scheduler.Models.Queues;

/// <summary>
/// Defines the persistance of the queue
/// </summary>
public sealed class ScheduleQueueItemPayload
{
    /// <summary>
    /// Defines the origin of the job
    /// </summary>
    public required JobExecutionOrigins JobExecutionOrigin { get; init; }

    /// <summary>
    /// The job object
    /// </summary>
    public required Job Job { get; set; }

    /// <summary>
    /// The method to execute with the parameters
    /// </summary>
    public required JobExecutionMetadata JobExecutionMetadata { get; init; }

    /// <summary>
    /// The schedule
    /// </summary>
    public required Schedule Schedule { get; set; }
}