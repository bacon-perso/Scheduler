using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Jobs;

namespace Bacon.Scheduler.Interfaces.Services.Jobs;

/// <summary>
/// Job store interface
/// </summary>
public interface IJobStore
{
    #region Insert

    /// <summary>
    /// Inserts a job
    /// </summary>
    /// <param name="job">The job to insert</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>Returns the job if successfully inserted</returns>
    ValueTask<Job?> InsertJobAsync(Job job, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts a list of jobs
    /// </summary>
    /// <param name="jobs">The list of jobs to insert</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The list of jobs successfully inserted</returns>
    ValueTask<IEnumerable<Job>> InsertJobsAsync(IEnumerable<Job> jobs, CancellationToken cancellationToken);

    #endregion Insert

    #region Update

    /// <summary>
    /// Update the job execution status
    /// </summary>
    /// <param name="jobId">The job ID</param>
    /// <param name="jobExecutionStatus">The job execution status to update</param>
    /// <param name="jobEndDate">The job end in UTC. Will be null as long as the job is not done</param>
    /// <param name="currentDate">The current date in UTC</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>returns the job if the job was successfully updated</returns>
    ValueTask<Job?> UpdateJobExecutionStatusAsync(Guid jobId, JobExecutionStatuses jobExecutionStatus, DateTime? jobEndDate, DateTime currentDate, CancellationToken cancellationToken);

    #endregion Update

    #region Delete

    /// <summary>
    /// Delete jobs related to the list of batch IDs sent. Should only be used if the data was not automatically handled through the delete schedule or delete batch.
    /// </summary>
    /// <param name="batchIds">A list of batch IDs</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    ValueTask DeleteJobsByBatchIdsAsync(IEnumerable<Guid> batchIds, CancellationToken cancellationToken = default);

    #endregion Delete
}