using Bacon.Scheduler.Models.Jobs;

namespace Bacon.Scheduler.Models.Schedules;

/// <summary>
/// Defines the wrapper class for the schedule and it's execution metadata
/// </summary>
public sealed class SchedulerWrapper
{
    /// <summary>
    /// Defines the schedule
    /// </summary>
    public required Schedule Schedule { get; init; }

    /// <summary>
    /// Defines the function that will be executed and it's arguments
    /// </summary>
    public required JobExecutionMetadata JobExecutionMetadata { get; set; }
}