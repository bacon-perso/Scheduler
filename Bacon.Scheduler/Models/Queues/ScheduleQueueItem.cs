namespace Bacon.Scheduler.Models.Queues;

/// <summary>
/// Schedule Queue
/// </summary>
public sealed class ScheduleQueueItem
{
    /// <summary>
    /// Schedule Queue ID
    /// </summary>
    public required Guid ScheduleQueueItemId { get; init; }

    /// <summary>
    /// The queue payload
    /// </summary>
    public required ScheduleQueueItemPayload Payload { get; set; }

    /// <summary>
    /// Queue creation date
    /// </summary>
    public required DateTime QueueCreationDate { get; init; }
}