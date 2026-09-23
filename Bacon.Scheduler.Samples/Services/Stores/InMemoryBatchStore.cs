using Bacon.Scheduler.Interfaces.Services.Batches;
using Bacon.Scheduler.Interfaces.Services.Jobs;
using Bacon.Scheduler.Models.Batches;

namespace Bacon.Scheduler.Samples.Services.Stores;

public class InMemoryBatchStore(IJobStore jobStore, InMemoryStores inMemoryStores) : IBatchStore
{
    #region Get

    public ValueTask<IDictionary<Guid, Batch?>> GetBatchesAsync(IEnumerable<Guid> batchIds, CancellationToken cancellationToken)
    {
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
        IDictionary<Guid, Batch?> batches = new Dictionary<Guid, Batch?>();
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
        foreach (Guid batchId in batchIds)
        {
            inMemoryStores.BatchesDic.TryGetValue(batchId, out Batch? batch);

            batches.Add(batchId, batch);
        }

        return ValueTask.FromResult(batches);
    }

    #endregion Get

    #region Insert

    public ValueTask<Batch?> InsertBatchAsync(Batch batch, CancellationToken cancellationToken)
    {
        Batch? insertedBatch = null;
        if (inMemoryStores.BatchesDic.TryAdd(batch.BatchId, batch))
        {
            insertedBatch = batch;
        }

        return ValueTask.FromResult(insertedBatch);
    }

    public ValueTask<IEnumerable<Batch>> InsertBatchesAsync(IEnumerable<Batch> batches, CancellationToken cancellationToken)
    {
        List<Batch> _batches = [];
        foreach (Batch batch in batches)
        {
            if (inMemoryStores.BatchesDic.TryAdd(batch.BatchId, batch))
            {
                _batches.Add(batch);
            }
        }

        return ValueTask.FromResult(_batches.AsEnumerable());
    }

    #endregion Insert

    #region Update

    public ValueTask<Batch?> UpdateBatchEndDateAsync(Guid batchId, DateTime currentDate, CancellationToken cancellationToken)
    {
        inMemoryStores.BatchesDic.TryGetValue(batchId, out Batch? batch);

        batch?.BatchEndDate = currentDate;

        return ValueTask.FromResult(batch);
    }

    #endregion Update

    #region Delete

    public async ValueTask DeleteBatchesByScheduleIdsAsync(IEnumerable<Guid> scheduleIds, CancellationToken cancellationToken)
    {
        List<Batch> batches = [];
        foreach (Guid scheduleId in scheduleIds)
        {
            batches.AddRange(inMemoryStores.BatchesDic.Where(w => w.Value.ScheduleId.Equals(scheduleId)).Select(s => s.Value));
        }

        batches = [.. batches.Distinct()];

        await jobStore.DeleteJobsByBatchIdsAsync(batches.Select(s => s.BatchId));

        foreach (Batch batch in batches)
        {
            inMemoryStores.BatchesDic.TryRemove(batch.BatchId, out _);
        }
    }

    #endregion Delete
}