using Bacon.Scheduler.Interfaces.Services.Batches;
using Bacon.Scheduler.Models.Batches;
using System.Collections.Concurrent;

namespace Bacon.Scheduler.Tests.Fakes;

internal sealed class FakeBatchStore : IBatchStore
{
    public readonly ConcurrentDictionary<Guid, Batch> Batches = [];

    public ValueTask<IDictionary<Guid, Batch?>> GetBatchesAsync(IEnumerable<Guid> batchIds, CancellationToken cancellationToken)
    {
        IDictionary<Guid, Batch?> result = new Dictionary<Guid, Batch?>();
        foreach (Guid batchId in batchIds)
        {
            Batches.TryGetValue(batchId, out Batch? batch);
            result[batchId] = batch;
        }

        return ValueTask.FromResult(result);
    }

    public ValueTask<Batch?> InsertBatchAsync(Batch batch, CancellationToken cancellationToken)
        => ValueTask.FromResult(Batches.TryAdd(batch.BatchId, batch) ? batch : null);

    public ValueTask<IEnumerable<Batch>> InsertBatchesAsync(IEnumerable<Batch> batches, CancellationToken cancellationToken)
    {
        List<Batch> inserted = [];
        foreach (Batch batch in batches)
        {
            if (Batches.TryAdd(batch.BatchId, batch))
            {
                inserted.Add(batch);
            }
        }

        return ValueTask.FromResult(inserted.AsEnumerable());
    }

    public ValueTask<Batch?> UpdateBatchEndDateAsync(Guid batchId, DateTime currentDate, CancellationToken cancellationToken)
    {
        if (!Batches.TryGetValue(batchId, out Batch? batch))
        {
            return ValueTask.FromResult<Batch?>(null);
        }

        batch.BatchEndDate = currentDate;
        return ValueTask.FromResult<Batch?>(batch);
    }

    public ValueTask DeleteBatchesByScheduleIdsAsync(IEnumerable<Guid> scheduleIds, CancellationToken cancellationToken)
    {
        foreach (Guid scheduleId in scheduleIds)
        {
            foreach (Batch batch in Batches.Values.Where(w => w.ScheduleId.Equals(scheduleId)).ToList())
            {
                Batches.TryRemove(batch.BatchId, out _);
            }
        }

        return ValueTask.CompletedTask;
    }
}
