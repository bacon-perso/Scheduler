using Bacon.Scheduler.Models.Batches;
using Bacon.Scheduler.Models.Schedules;

namespace Bacon.Scheduler.Interfaces.Services.Batches;

internal interface IBatchService
{
    #region Get

    Task<IEnumerable<Batch>> GetBatchesAsync(IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken);
    
    #endregion Get

    #region Insert

    Task<Batch> InsertBatchAsync(Schedule schedule, DateTime currentDate, CancellationToken cancellationToken);

    Task<IEnumerable<Batch>> InsertBatchesAsync(IEnumerable<Schedule> schedules, DateTime currentDate, CancellationToken cancellationToken);

    #endregion Insert

    #region Update

    Task<Batch> UpdateBatchEndDateAsync(Guid batchId, DateTime currentDate, CancellationToken cancellationToken);

    #endregion Update
}