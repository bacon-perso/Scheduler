using Bacon.Scheduler.Models.Batches;

namespace Bacon.Scheduler.Interfaces.Services.Batches;

/// <summary>
/// Defines the customizable store for the <see cref="Batch"/>
/// </summary>
public interface IBatchStore
{
    #region Get

    /// <summary>
    /// Returns a list of batch from their IDs
    /// </summary>
    /// <param name="batchIds">The list of batch IDs</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>A dictionary of batches. Each key of the dictionary should represent each batch ID sent. The value represents the batch found and should be null if not found</returns>
    ValueTask<IDictionary<Guid, Batch?>> GetBatchesAsync(IEnumerable<Guid> batchIds, CancellationToken cancellationToken);

    #endregion Get

    #region Insert

    /// <summary>
    /// Insert a batch
    /// </summary>
    /// <param name="batch">The batch to insert</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The inserted batch if successful. Null if not</returns>
    ValueTask<Batch?> InsertBatchAsync(Batch batch, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts a list of batch
    /// </summary>
    /// <param name="batches">The list of batch to insert</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The newly added list of batch. Should only contain the successful inserts</returns>
    ValueTask<IEnumerable<Batch>> InsertBatchesAsync(IEnumerable<Batch> batches, CancellationToken cancellationToken);

    #endregion Insert

    #region Update

    /// <summary>
    /// Updates a batch end date with the current date
    /// </summary>
    /// <param name="batchId">The batch ID</param>
    /// <param name="currentDate">The current date in UTC</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The updated batch if successful. Null if not</returns>
    ValueTask<Batch?> UpdateBatchEndDateAsync(Guid batchId, DateTime currentDate, CancellationToken cancellationToken);

    #endregion Update

    #region Delete

    /// <summary>
    /// Deletes all batches tied to the schedule. Should be executed if the DeleteSchedules in the schedule store did not delete them
    /// </summary>
    /// <param name="scheduleIds">The list of schedule IDs</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    ValueTask DeleteBatchesByScheduleIdsAsync(IEnumerable<Guid> scheduleIds, CancellationToken cancellationToken);

    #endregion Delete
}