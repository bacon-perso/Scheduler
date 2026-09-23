using System.Text.Json.Serialization;

namespace Bacon.Scheduler.Models.Schedules;

/// <summary>
/// Schedule Class
/// </summary>
public sealed class Schedule
{
    /// <summary>
    /// The schedule unique identifier
    /// </summary>
    public required Guid ScheduleId { get; init; }

    /// <summary>
    /// The schedule's unique name
    /// </summary>
    public required string ScheduleName { get; set; }

    /// <summary>
    /// Defines if the schedule was created by the system
    /// </summary>
    public bool IsSystem { get; init; }

    /// <summary>
    /// Defines if the schedule is active or not
    /// </summary>
    public bool IsActive { get; set; }

    #region Recurrence

    /// <summary>
    /// The schedule recurrence configurations
    /// </summary>
    [JsonPropertyName("schedule_recurrence_configs")]
    public required ScheduleRecurrenceConfigs ScheduleRecurrenceConfig { get; init; }

    /// <summary>
    /// The current schedule's execution status
    /// </summary>
    public ScheduleExecutionStatuses ScheduleExecutionStatus { get; set; }

    /// <summary>
    /// Defines the last time the schedule was executed
    /// </summary>
    public DateTime? LastOccurrenceDate { get; set; }

    /// <summary>
    /// Defines the last occurrence status
    /// </summary>
    public ScheduleOccurrenceStatuses LastOccurrenceStatus { get; set; }

    /// <summary>
    /// Defines the next time the schedule is going to be executed
    /// </summary>
    public DateTime? NextOccurrenceDate { get; set; }

    #endregion Recurrence

    #region Retry

    /// <summary>
    /// Defines the retry configs
    /// </summary>
    [JsonPropertyName("schedule_retry_configs")]
    public required ScheduleRetryConfigs ScheduleRetryConfig { get; init; }

    #endregion Retry

    /// <summary>
    /// The date of the object's creation
    /// </summary>
    public DateTime CreationDate { get; internal init; }

    /// <summary>
    /// The last modification date of the object
    /// </summary>
    public DateTime? ModificationDate { get; set; }
}