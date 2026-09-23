using Bacon.Scheduler.Interfaces.Services.Jobs;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Jobs;
using System.Collections.Concurrent;

namespace Bacon.Scheduler.Tests.Fakes;

internal sealed class FakeJobStore : IJobStore
{
    public readonly ConcurrentDictionary<Guid, Job> Jobs = [];

    public ValueTask<Job?> InsertJobAsync(Job job, CancellationToken cancellationToken)
        => ValueTask.FromResult(Jobs.TryAdd(job.JobId, job) ? job : null);

    public ValueTask<IEnumerable<Job>> InsertJobsAsync(IEnumerable<Job> jobs, CancellationToken cancellationToken)
    {
        List<Job> inserted = [];
        foreach (Job job in jobs)
        {
            if (Jobs.TryAdd(job.JobId, job))
            {
                inserted.Add(job);
            }
        }

        return ValueTask.FromResult(inserted.AsEnumerable());
    }

    public ValueTask<Job?> UpdateJobExecutionStatusAsync(Guid jobId, JobExecutionStatuses jobExecutionStatus, DateTime? jobEndDate, DateTime currentDate, CancellationToken cancellationToken)
    {
        if (!Jobs.TryGetValue(jobId, out Job? job))
        {
            return ValueTask.FromResult<Job?>(null);
        }

        job.JobExecutionStatus = jobExecutionStatus;
        job.JobEndDate = jobEndDate;
        job.ModificationDate = currentDate;
        return ValueTask.FromResult<Job?>(job);
    }

    public ValueTask DeleteJobsByBatchIdsAsync(IEnumerable<Guid> batchIds, CancellationToken cancellationToken = default)
    {
        foreach (Guid batchId in batchIds)
        {
            foreach (Job job in Jobs.Values.Where(w => w.BatchId.Equals(batchId)).ToList())
            {
                Jobs.TryRemove(job.JobId, out _);
            }
        }

        return ValueTask.CompletedTask;
    }
}
