using Bacon.Scheduler.Interfaces.Services.Jobs;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Jobs;

namespace Bacon.Scheduler.Samples.Services.Stores;

public class InMemoryJobStore(InMemoryStores inMemoryStores) : IJobStore
{
    #region Inserts

    public ValueTask<Job?> InsertJobAsync(Job job, CancellationToken cancellationToken)
    {
        Job? _job = null;
        if (inMemoryStores.JobsDic.TryAdd(job.JobId, job))
        {
            _job = job;
        }

        return ValueTask.FromResult(_job);
    }

    public ValueTask<IEnumerable<Job>> InsertJobsAsync(IEnumerable<Job> jobs, CancellationToken cancellationToken)
    {
        List<Job> _jobs = [];
        foreach (Job job in jobs)
        {
            if (inMemoryStores.JobsDic.TryAdd(job.JobId, job))
            {
                _jobs.Add(job);
            }
        }

        return ValueTask.FromResult(_jobs.AsEnumerable());
    }

    #endregion Inserts

    #region Updates

    public ValueTask<Job?> UpdateJobExecutionStatusAsync(Guid jobId, JobExecutionStatuses jobExecutionStatus, DateTime? jobEndDate, DateTime currentDate, CancellationToken cancellationToken)
    {
        inMemoryStores.JobsDic.TryGetValue(jobId, out Job? job);

        job?.JobExecutionStatus = jobExecutionStatus;
        job?.JobEndDate = jobEndDate;
        job?.ModificationDate = currentDate;

        return ValueTask.FromResult(job);
    }

    #endregion Updates

    #region Deletes

    public ValueTask DeleteJobsByBatchIdsAsync(IEnumerable<Guid> batchIds, CancellationToken cancellationToken = default)
    {
        List<Job> jobs = [];
        foreach (Guid batchId in batchIds)
        {
            jobs.AddRange(inMemoryStores.JobsDic.Where(w => w.Value.BatchId.Equals(batchId)).Select(s => s.Value));
        }

        jobs = [.. jobs.Distinct()];

        foreach (Job job in jobs)
        {
            inMemoryStores.JobsDic.TryRemove(job.JobId, out _);
        }

        return ValueTask.CompletedTask;
    }

    #endregion Deletes
}