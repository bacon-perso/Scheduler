namespace Bacon.Scheduler.Models.Jobs;

/// <summary>
/// Defines a job. Each execution is tied to a job
/// </summary>
public sealed class Job
{
    /// <summary>
    /// Defines the unique identifier of the job
    /// </summary>
    public required Guid JobId { get; init; }

    /// <summary>
    /// Defines the unique identifier of the batch that the job is linked to
    /// </summary>
    public required Guid BatchId { get; init; }

    /// <summary>
    /// Defines the current availability status of the data transformation
    /// </summary>
    public required JobExecutionStatuses JobExecutionStatus { get; set; }

    /// <summary>
    /// Defines the execution start date of the job
    /// </summary>
    public required DateTime JobStartDate { get; init; }

    /// <summary>
    /// Defines the execution end date of the job
    /// </summary>
    public DateTime? JobEndDate { get; set; }

    /// <summary>
    /// Defines the date the job  was created
    /// </summary>
    public required DateTime CreationDate { get; init; }

    /// <summary>
    /// Defines the last modification date of the job
    /// </summary>
    public DateTime? ModificationDate { get; set; }
}