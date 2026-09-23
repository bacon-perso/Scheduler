using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Batches;
using Bacon.Scheduler.Models.Jobs;

namespace Bacon.Scheduler.Interfaces.Services.Jobs;

internal interface IJobService
{
    #region Insert

    internal Task<Job> InsertJobAsync(Batch batch, CancellationToken cancellationToken);

    internal Task<IEnumerable<Job>> InsertJobsAsync(IEnumerable<Guid> batchIds, DateTime currentDate, CancellationToken cancellationToken);

    #endregion Insert

    #region Update

    internal Task<Job> UpdateJobExecutionStatusAsync(Guid jobId, JobExecutionStatuses jobExecutionStatus, DateTime currentDate, CancellationToken cancellationToken);

    #endregion Update
}