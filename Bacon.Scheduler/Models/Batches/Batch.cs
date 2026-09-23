namespace Bacon.Scheduler.Models.Batches;

/// <summary>
/// Represents a batch. Batch can come from a schedule execution or individual execution
/// </summary>
public sealed class Batch
{
    /// <summary>
    /// The batch ID
    /// </summary>
    public required Guid BatchId { get; init; }

    /// <summary>
    /// The optional schedule ID
    /// </summary>
    public required Guid ScheduleId { get; init; }

    /// <summary>
    /// The start date of the batch
    /// </summary>
    public required DateTime BatchStartDate { get; init; }

    /// <summary>
    /// The end date of the batch
    /// </summary>
    public DateTime? BatchEndDate { get; set; }
}