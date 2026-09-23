using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Queues;

namespace Bacon.Scheduler.Interfaces.Services.Queues;

/// <summary>
/// Interfaces for the schedule queue store
/// </summary>
public interface IScheduleQueueStore
{
    #region Get

    /// <summary>
    /// Returns the number of items currently in the queue
    /// </summary>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns></returns>
    ValueTask<int> CountAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns the first element respecting the order given
    /// </summary>
    /// <param name="scheduleQueueOrder"></param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>Returns the first element. Returns null if the queue is empty</returns>
    ValueTask<ScheduleQueueItem?> PeekScheduleQueuePayloadAsync(ScheduleQueueOrders scheduleQueueOrder, CancellationToken cancellationToken);

    #endregion Get

    #region Insert

    /// <summary>
    /// Insert a list of schedule queue payloads in the schedule queue
    /// </summary>
    /// <param name="scheduleQueueItemPayloads"></param>
    /// <param name="currentDate">The current date in UTC</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The list of successful inserted schedule queue</returns>
    ValueTask<IEnumerable<ScheduleQueueItem>> InsertSchedulePayloadsAsync(IEnumerable<ScheduleQueueItemPayload> scheduleQueueItemPayloads, DateTime currentDate, CancellationToken cancellationToken);

    #endregion Insert

    #region Delete

    /// <summary>
    /// Delete an item from the schedule queue
    /// </summary>
    /// <param name="scheduleQueueId">The schedule queue ID</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    ValueTask DeleteScheduleQueueAsync(Guid scheduleQueueId, CancellationToken cancellationToken);

    #endregion Delete
}