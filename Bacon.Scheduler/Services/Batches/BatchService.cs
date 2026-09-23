using Bacon.Scheduler.Extensions;
using Bacon.Scheduler.Interfaces.Services.Batches;
using Bacon.Scheduler.Models.Batches;
using Bacon.Scheduler.Models.Schedules;

namespace Bacon.Scheduler.Services.Batches;

internal sealed class BatchService(IBatchStore batchStore) : IBatchService
{
    #region Get

    public async Task<IEnumerable<Batch>> GetBatchesAsync(IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken)
    {
        IDictionary<Guid, Batch?> batchesDic = await batchStore.GetBatchesAsync(batchIds, cancellationToken);

        (IReadOnlyCollection<Batch> found, IReadOnlyCollection<Guid> missing) = batchesDic.PartitionLookupData(batchIds);

        if (missing.Count != 0)
        {
            throw new KeyNotFoundException($"Scheduler - The following batches could not be found: '{string.Join(", ", missing)}'");
        }

        return found;
    }

    #endregion Get

    #region Insert

    public async Task<Batch> InsertBatchAsync(Schedule schedule, DateTime currentDate, CancellationToken cancellationToken)
    {
        Batch batch = new()
        {
            BatchId = Guid.NewGuid(),
            BatchStartDate = currentDate,
            BatchEndDate = null,
            ScheduleId = schedule.ScheduleId
        };

        Batch? insertedBatch = await batchStore.InsertBatchAsync(batch, cancellationToken).ConfigureAwait(false);

        return insertedBatch ?? throw new KeyNotFoundException($"Scheduler - The batch could not be inserted");
    }

    public async Task<IEnumerable<Batch>> InsertBatchesAsync(IEnumerable<Schedule> schedules, DateTime currentDate, CancellationToken cancellationToken)
    {
        List<Batch> batches = [];
        foreach (Schedule schedule in schedules)
        {
            batches.Add(new()
            {
                BatchId = Guid.NewGuid(),
                BatchStartDate = currentDate,
                BatchEndDate = null,
                ScheduleId = schedule.ScheduleId
            });
        }

        IEnumerable<Batch> returnedBatches = await batchStore.InsertBatchesAsync(batches, cancellationToken);

        IEnumerable<Guid> invalidBatches = batches.Select(s => s.ScheduleId).Except(returnedBatches.Select(s => s.ScheduleId));
        if (invalidBatches.Any())
        {
            throw new KeyNotFoundException($"Scheduler - The batches could not be created for the following schedules: '{string.Join(", ", invalidBatches)}'");
        }

        return returnedBatches;
    }

    #endregion Insert

    #region Update

    public async Task<Batch> UpdateBatchEndDateAsync(Guid batchId, DateTime currentDate, CancellationToken cancellationToken)
    {
        Batch? batch = await batchStore.UpdateBatchEndDateAsync(batchId, currentDate, cancellationToken);

        return batch ?? throw new KeyNotFoundException($"Scheduler - The batch end date could not be updated. The batch '{batchId}' cannot be found");
    }

    #endregion Update
}