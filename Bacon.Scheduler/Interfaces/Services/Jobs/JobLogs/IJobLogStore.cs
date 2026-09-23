using Bacon.Scheduler.Models.Jobs;

namespace Bacon.Scheduler.Interfaces.Services.Jobs.JobLogs;

/// <summary>
/// Job Log store
/// </summary>
public interface IJobLogStore
{
    /// <summary>
    /// Insert a job log after a schedule finishes executing an occurence.
    /// </summary>
    /// <param name="job">The job that executed</param>
    /// <param name="jobExecutionResult">The job execution result returned by the job execution</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns></returns>
    ValueTask InsertJobLogAsync(Job job, JobExecutionResult jobExecutionResult, CancellationToken cancellationToken);
}