using Bacon.Scheduler.Interfaces.Services.Jobs;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Batches;
using Bacon.Scheduler.Models.Jobs;

namespace Bacon.Scheduler.Services.Jobs;

internal sealed class JobService(IJobStore jobStore) : IJobService
{
    #region Insert

    public async Task<Job> InsertJobAsync(Batch batch, CancellationToken cancellationToken)
    {
        Job job = new()
        {
            JobId = Guid.NewGuid(),
            BatchId = batch.BatchId,
            JobStartDate = batch.BatchStartDate,
            JobEndDate = null,
            JobExecutionStatus = JobExecutionStatuses.Queued,
            CreationDate = batch.BatchStartDate,
            ModificationDate = null
        };

        Job? insertedJob = await jobStore.InsertJobAsync(job, cancellationToken).ConfigureAwait(false);

        return insertedJob ?? throw new KeyNotFoundException($"Scheduler - The job could not be inserted");
    }

    public async Task<IEnumerable<Job>> InsertJobsAsync(IEnumerable<Guid> batchIds, DateTime currentDate, CancellationToken cancellationToken)
    {
        List<Job> jobs = [];
        foreach (Guid batchId in batchIds)
        {
            jobs.Add(new()
            {
                JobId = Guid.NewGuid(),
                BatchId = batchId,
                JobStartDate = currentDate,
                JobEndDate = null,
                JobExecutionStatus = JobExecutionStatuses.Queued,
                CreationDate = currentDate,
                ModificationDate = null
            });
        }

        IEnumerable<Job> insertedJobs = await jobStore.InsertJobsAsync(jobs, cancellationToken);

        IEnumerable<Guid> invalidBatches = jobs.Select(s => s.BatchId).Except(insertedJobs.Select(s => s.BatchId));
        if (invalidBatches.Any())
        {
            throw new KeyNotFoundException($"Scheduler - The jobs could not be created for the following batches: '{string.Join(", ", invalidBatches)}'");
        }

        return insertedJobs;
    }

    #endregion Insert

    #region Update

    public async Task<Job> UpdateJobExecutionStatusAsync(Guid jobId, JobExecutionStatuses jobExecutionStatus, DateTime currentDate, CancellationToken cancellationToken)
    {
        DateTime? jobEndDate = jobExecutionStatus switch
        {
            JobExecutionStatuses.Success or JobExecutionStatuses.Error or JobExecutionStatuses.Critical => currentDate,
            _ => null
        };

        Job? updatedJob = await jobStore.UpdateJobExecutionStatusAsync(jobId, jobExecutionStatus, jobEndDate, currentDate, cancellationToken);

        return updatedJob ?? throw new KeyNotFoundException($"Scheduler - The jobs status could not be updated. The job '{jobId}' cannot be found");
    }

    #endregion Update
}