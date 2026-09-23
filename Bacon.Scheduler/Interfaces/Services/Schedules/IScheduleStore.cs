using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Models.Schedules;

namespace Bacon.Scheduler.Interfaces.Services.Schedules;

/// <summary>
/// Schedule Store
/// </summary>
public interface IScheduleStore
{
    #region Get

    /// <summary>
    /// Returns a schedule by ID
    /// </summary>
    /// <param name="scheduleId">The schedule ID</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>A schedule if found, null if not</returns>
    ValueTask<Schedule?> GetScheduleAsync(Guid scheduleId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a list of schedules IDs
    /// </summary>
    /// <param name="scheduleIds">The list of schedule IDs</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>A dictionary with the keys containing the schedule IDs passed in parameters. The value will be the found schedule or null if not found</returns>
    ValueTask<IDictionary<Guid, Schedule?>> GetSchedulesAsync(IEnumerable<Guid> scheduleIds, CancellationToken cancellationToken);

    /// <summary>
    /// Returns all schedules configured in the system
    /// </summary>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns></returns>
    ValueTask<IEnumerable<Schedule>> GetAllSchedulesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns the schedule execution metadata
    /// </summary>
    /// <param name="scheduleId">The schedule ID</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The schedule execution metadata if found. If the schedule or the execution metadata is not found, return null</returns>
    ValueTask<JobExecutionMetadata?> GetScheduleJobMetadataAsync(Guid scheduleId, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if the schedule name already exists
    /// </summary>
    /// <param name="scheduleId"></param>
    /// <param name="scheduleName"></param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>True if the schedule name exists, false if it does not or if the scheduleId is the same</returns>
    ValueTask<bool> CheckScheduleNameExistsAsync(Guid? scheduleId, string scheduleName, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the next schedule to insert in the queue handled on a timer.
    /// The schedules returned should be active AND ((the schedule is ready AND (the NextScheduleOccurrenceDate is smaller or equal to now OR NextScheduleOccurrenceDate is null)) OR (the schedule is pending AND the NextScheduleRetryDate is smaller or equal to now))
    /// </summary>
    /// <param name="currentDate">The current date</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns></returns>
    ValueTask<IEnumerable<SchedulerWrapper>> GetSchedulesToRunAsync(DateTime currentDate, CancellationToken cancellationToken);

    #endregion Get

    #region Insert

    /// <summary>
    /// Inserts a schedules and it's execution metadata
    /// </summary>
    /// <param name="schedulerWrapper">The schedule wrapper containing the schedule and it's execution metadata</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The inserted schedule if successful. Null if not</returns>
    ValueTask<SchedulerWrapper?> InsertScheduleAsync(SchedulerWrapper schedulerWrapper, CancellationToken cancellationToken);

    #endregion Insert

    #region Update

    /// <summary>
    /// Updates the schedule name
    /// </summary>
    /// <param name="scheduleId">The schedule ID</param>
    /// <param name="scheduleName">The new unique schedule name</param>
    /// <param name="currentDate">The current date</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The updated schedule if successful. Null if not</returns>
    ValueTask<Schedule?> UpdateScheduleNameAsync(Guid scheduleId, string scheduleName, DateTime currentDate, CancellationToken cancellationToken);

    /// <summary>
    /// Updates a schedule status
    /// </summary>
    /// <param name="scheduleId">The schedule ID</param>
    /// <param name="isActive">The schedule status (active/inactive)</param>
    /// <param name="nextScheduleOccurrenceDate">The next schedule occurence date. Null means the schedule will not run again</param>
    /// <param name="currentDate">The current date</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The updated schedule if successful. Null if not</returns>
    ValueTask<Schedule?> UpdateScheduleStatusAsync(Guid scheduleId, bool isActive, DateTime? nextScheduleOccurrenceDate, DateTime currentDate, CancellationToken cancellationToken);

    /// <summary>
    /// Update the schedule recurrence configurations
    /// </summary>
    /// <param name="scheduleId">The schedule ID</param>
    /// <param name="scheduleRecurrenceType">The type of recurrence</param>
    /// <param name="startDate">The schedule start date in UTC. If left null, the current date will be used</param>
    /// <param name="endDate">The schedule end date in UTC. If null, then no end date</param>
    /// <param name="everyX">Depending on the scheduleRecurrenceType, will mean every X hours/days/weeks/months (IE: every 3 days)</param>
    /// <param name="weekdays">The optional list of weekdays. Can only be used if the scheduleRecurrenceType is weekly</param>
    /// <param name="dayOfTheMonths">The optional list of days. Accepts [1-31] and 'L' for the last day of the month. Can only be used if the scheduleRecurrenceType is monthly</param>
    /// <param name="nextScheduleOccurrenceDate">The next schedule occurence date. Null means the schedule will not run again</param>
    /// <param name="currentDate">The current date</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The updated schedule if successful. Null if not</returns>
    ValueTask<Schedule?> UpdateScheduleRecurrenceConfigsAsync(Guid scheduleId, ScheduleRecurrenceTypes scheduleRecurrenceType, DateTime startDate, DateTime? endDate, short everyX, IEnumerable<Weekdays>? weekdays, IEnumerable<string>? dayOfTheMonths
        , DateTime? nextScheduleOccurrenceDate, DateTime currentDate, CancellationToken cancellationToken);

    /// <summary>
    /// Update the schedule retry configurations
    /// </summary>
    /// <param name="scheduleId">The schedule ID</param>
    /// <param name="autoRestartOnFailure">If set to true, will reset the schedule status to ready after a failure (reached the retry limit). Otherwise, the schedule will stay in a failed state until manually started</param>
    /// <param name="retryLimit">Configure the amount of retries allowed. 0 for no retry, 10 for max retries. Retries are following a linear mechanism of 1min * retry count</param>
    /// <param name="currentDate">The current date</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The updated schedule if successful. Null if not</returns>
    ValueTask<Schedule?> UpdateScheduleRetryConfigsAsync(Guid scheduleId, bool autoRestartOnFailure, byte retryLimit, DateTime currentDate, CancellationToken cancellationToken);

    /// <summary>
    /// Updates schedule execution status
    /// </summary>
    /// <param name="scheduleId">The schedule ID</param>
    /// <param name="scheduleExecutionStatus">The schedule execution status</param>
    /// <param name="currentDate">The current date</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The updated schedule if successful. Null if not</returns>
    ValueTask<Schedule?> UpdateScheduleExecutionStatusAsync(Guid scheduleId, ScheduleExecutionStatuses scheduleExecutionStatus, DateTime currentDate, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the schedule job execution metadata, to ensure the correct namespace are used
    /// </summary>
    /// <param name="scheduleJobExecutionMetadatas">Dictionary with the schedule IDs as key, and their JobExecutionMetadata to update</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>A dictionary with the keys containing the schedule IDs passed in parameters. The value will be the found JobExecutionMetadata or null if not found</returns>
    ValueTask<IDictionary<Guid, JobExecutionMetadata?>> UpdateScheduleJobExecutionMetadataAsync(IDictionary<Guid, JobExecutionMetadata> scheduleJobExecutionMetadatas, CancellationToken cancellationToken);

    /// <summary>
    /// Updates schedule execution status
    /// </summary>
    /// <param name="scheduleIds">The list of schedule IDs</param>
    /// <param name="scheduleExecutionStatus">The schedule execution status</param>
    /// <param name="currentDate">The current date</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The updated schedule wrapper if successful. Null if not</returns>
    ValueTask<IDictionary<Guid, Schedule?>> UpdateSchedulesExecutionStatusAsync(IEnumerable<Guid> scheduleIds, ScheduleExecutionStatuses scheduleExecutionStatus, DateTime currentDate, CancellationToken cancellationToken);

    /// <summary>
    /// Updates schedule retry count
    /// </summary>
    /// <param name="scheduleId">The schedule ID</param>
    /// <param name="scheduleRetryCount">The schedule retry count</param>
    /// <param name="nextRetryDate">The next schedule retry date. Null if the retry count is being reset</param>
    /// <param name="currentDate">The current date</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The updated schedule if successful. Null if not</returns>
    ValueTask<Schedule?> UpdateScheduleRetryCountAsync(Guid scheduleId, byte scheduleRetryCount, DateTime? nextRetryDate, DateTime currentDate, CancellationToken cancellationToken);

    /// <summary>
    /// Return maintenance schedule.
    /// </summary>
    /// <param name="scheduleId">The schedule ID</param>
    /// <param name="nextScheduleOccurrenceDate">The next schedule occurrence date. Null if the schedule reached its end date</param>
    /// <param name="currentDate">The current date</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The updated schedule if successful. Null if not</returns>
    ValueTask<Schedule?> UpdateScheduleNextOccurrenceDateAsync(Guid scheduleId, DateTime? nextScheduleOccurrenceDate, DateTime currentDate, CancellationToken cancellationToken);

    /// <summary>
    /// Update the schedule last run status and date
    /// </summary>
    /// <param name="scheduleId">The schedule ID</param>
    /// <param name="lastScheduleOccurrenceStatus">The last schedule occurrence status</param>
    /// <param name="lastScheduleOccurrenceDate">The last schedule occurrence date</param>
    /// <param name="currentDate">The current date</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The updated schedule if successful. Null if not</returns>
    ValueTask<Schedule?> UpdateScheduleLastRunStatusAndDateAsync(Guid scheduleId, ScheduleOccurrenceStatuses lastScheduleOccurrenceStatus, DateTime lastScheduleOccurrenceDate, DateTime currentDate, CancellationToken cancellationToken);

    /// <summary>
    /// Update the schedule with the current batch ID so it can be linked to a new job on it's next retry
    /// </summary>
    /// <param name="scheduleId">The schedule ID</param>
    /// <param name="batchId">The optional batch ID</param>
    /// <param name="currentDate">The current date</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns></returns>
    ValueTask<Schedule?> UpdateScheduleCurrentRunningBatchIdAsync(Guid scheduleId, Guid? batchId, DateTime currentDate, CancellationToken cancellationToken);

    #endregion Update

    #region Delete

    /// <summary>
    /// Delete schedules. All related items can be deleted, or other functions can be used to delete the associated items
    /// </summary>
    /// <param name="scheduleIds">The list of schedule IDs</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    ValueTask DeleteSchedulesAsync(IEnumerable<Guid> scheduleIds, CancellationToken cancellationToken);

    #endregion Delete
}