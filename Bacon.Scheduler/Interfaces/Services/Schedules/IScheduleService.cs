using Bacon.Scheduler.Interfaces.Services.Jobs.JobLogs;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Models.Schedules;
using System.Data;
using System.Linq.Expressions;

namespace Bacon.Scheduler.Interfaces.Services.Schedules;

/// <summary>
/// Schedule service
/// </summary>
public interface IScheduleService
{
    #region Get

    /// <summary>
    /// Gets a schedule by ID
    /// </summary>
    /// <param name="scheduleId">The schedule ID</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>Returns the schedule</returns>
    /// <exception cref="KeyNotFoundException">Will be thrown if the schedule does not exist</exception>
    Task<Schedule> GetScheduleAsync(Guid scheduleId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a list of schedules by ID
    /// </summary>
    /// <param name="scheduleIds">The list of schedule IDs</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>Returns the list of schedule</returns>
    /// <exception cref="KeyNotFoundException">Will be thrown if one or many schedule do not exist</exception>
    Task<IEnumerable<Schedule>> GetSchedulesAsync(IReadOnlyCollection<Guid> scheduleIds, CancellationToken cancellationToken);

    #endregion Get

    #region Insert

    #region Insert Schedule

    /// <summary>
    /// Inserts a schedule in the system
    /// </summary>
    /// <param name="scheduleName">The unique schedule name</param>
    /// <param name="scheduleRecurrenceType">The type of recurrence</param>
    /// <param name="startDate">The schedule start date in UTC. If left null, the current date will be used</param>
    /// <param name="endDate">The schedule end date in UTC. If null, then no end date</param>
    /// <param name="everyX">Depending on the scheduleRecurrenceType, will mean every X hours/days/weeks/months (IE: every 3 days)</param>
    /// <param name="weekdays">The optional list of weekdays. Can only be used if the scheduleRecurrenceType is weekly</param>
    /// <param name="dayOfTheMonths">The optional list of days. Accepts [1-31] and 'L' for the last day of the month. Can only be used if the scheduleRecurrenceType is monthly</param>
    /// <param name="autoRestartOnFailure">If set to true, will reset the schedule status to ready after a failure (reached the retry limit). Otherwise, the schedule will stay in a failed state until manually started</param>
    /// <param name="retryLimit">Configure the amount of retries allowed. 0 for no retry, 10 for max retries. Retries are following a linear mechanism of 1min * retry count</param>
    /// <param name="expression">The function to call, including the parameters</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The inserted schedule</returns>
    /// <exception cref="ArgumentException">Will be thrown if the expression is not a method call, or if other configuration are invalid</exception>
    /// <exception cref="InvalidOperationException">Will be thrown if the expression passed cannot be compiled</exception>
    /// <exception cref="KeyNotFoundException">Will be thrown if the schedule could not be properly inserted and returned as null through the store</exception>
    /// <exception cref="ArgumentOutOfRangeException">Will be thrown if the date configurations are invalid</exception>
    /// <exception cref="DuplicateNameException">Will be thrown if the schedule name already exist</exception>
    Task<Schedule> InsertScheduleAsync<TJobLogStore>(string scheduleName, ScheduleRecurrenceTypes scheduleRecurrenceType, DateTime? startDate, DateTime? endDate, short everyX, IEnumerable<Weekdays>? weekdays, IEnumerable<string>? dayOfTheMonths
        , bool autoRestartOnFailure, byte retryLimit, [InstantHandle] Expression<Func<JobExecutionResult>> expression, CancellationToken cancellationToken)
        where TJobLogStore : class, IJobLogStore;

    /// <summary>
    /// Inserts a schedule in the system
    /// </summary>
    /// <param name="scheduleName">The unique schedule name</param>
    /// <param name="scheduleRecurrenceType">The type of recurrence</param>
    /// <param name="startDate">The schedule start date in UTC. If left null, the current date will be used</param>
    /// <param name="endDate">The schedule end date in UTC. If null, then no end date</param>
    /// <param name="everyX">Depending on the scheduleRecurrenceType, will mean every X hours/days/weeks/months (IE: every 3 days)</param>
    /// <param name="weekdays">The optional list of weekdays. Can only be used if the scheduleRecurrenceType is weekly</param>
    /// <param name="dayOfTheMonths">The optional list of days. Accepts [1-31] and 'L' for the last day of the month. Can only be used if the scheduleRecurrenceType is monthly</param>
    /// <param name="autoRestartOnFailure">If set to true, will reset the schedule status to ready after a failure (reached the retry limit). Otherwise, the schedule will stay in a failed state until manually started</param>
    /// <param name="retryLimit">Configure the amount of retries allowed. 0 for no retry, 10 for max retries. Retries are following a linear mechanism of 1min * retry count</param>
    /// <param name="expression">The function to call, including the parameters</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The inserted schedule</returns>
    /// <exception cref="ArgumentException">Will be thrown if the expression is not a method call, or if other configuration are invalid</exception>
    /// <exception cref="InvalidOperationException">Will be thrown if the expression passed cannot be compiled</exception>
    /// <exception cref="KeyNotFoundException">Will be thrown if the schedule could not be properly inserted and returned as null through the store</exception>
    /// <exception cref="ArgumentOutOfRangeException">Will be thrown if the date configurations are invalid</exception>
    /// <exception cref="DuplicateNameException">Will be thrown if the schedule name already exist</exception>
    Task<Schedule> InsertScheduleAsync<TJobLogStore>(string scheduleName, ScheduleRecurrenceTypes scheduleRecurrenceType, DateTime? startDate, DateTime? endDate, short everyX, IEnumerable<Weekdays>? weekdays, IEnumerable<string>? dayOfTheMonths
        , bool autoRestartOnFailure, byte retryLimit, [InstantHandle] Expression<Func<Task<JobExecutionResult>>> expression, CancellationToken cancellationToken)
        where TJobLogStore : class, IJobLogStore;

    /// <summary>
    /// Inserts a schedule in the system
    /// </summary>
    /// <param name="scheduleName">The unique schedule name</param>
    /// <param name="scheduleRecurrenceType">The type of recurrence</param>
    /// <param name="startDate">The schedule start date in UTC. If left null, the current date will be used</param>
    /// <param name="endDate">The schedule end date in UTC. If null, then no end date</param>
    /// <param name="everyX">Depending on the scheduleRecurrenceType, will mean every X hours/days/weeks/months (IE: every 3 days)</param>
    /// <param name="weekdays">The optional list of weekdays. Can only be used if the scheduleRecurrenceType is weekly</param>
    /// <param name="dayOfTheMonths">The optional list of days. Accepts [1-31] and 'L' for the last day of the month. Can only be used if the scheduleRecurrenceType is monthly</param>
    /// <param name="autoRestartOnFailure">If set to true, will reset the schedule status to ready after a failure (reached the retry limit). Otherwise, the schedule will stay in a failed state until manually started</param>
    /// <param name="retryLimit">Configure the amount of retries allowed. 0 for no retry, 10 for max retries. Retries are following a linear mechanism of 1min * retry count</param>
    /// <param name="expression">The function to call, including the parameters</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The inserted schedule</returns>
    /// <exception cref="ArgumentException">Will be thrown if the expression is not a method call, or if other configuration are invalid</exception>
    /// <exception cref="InvalidOperationException">Will be thrown if the expression passed cannot be compiled</exception>
    /// <exception cref="KeyNotFoundException">Will be thrown if the schedule could not be properly inserted and returned as null through the store</exception>
    /// <exception cref="ArgumentOutOfRangeException">Will be thrown if the date configurations are invalid</exception>
    /// <exception cref="DuplicateNameException">Will be thrown if the schedule name already exist</exception>
    Task<Schedule> InsertScheduleAsync<TJobLogStore, TItem>(string scheduleName, ScheduleRecurrenceTypes scheduleRecurrenceType, DateTime? startDate, DateTime? endDate, short everyX, IEnumerable<Weekdays>? weekdays, IEnumerable<string>? dayOfTheMonths
        , bool autoRestartOnFailure, byte retryLimit, [InstantHandle] Expression<Func<TItem, JobExecutionResult>> expression, CancellationToken cancellationToken)
        where TJobLogStore : class, IJobLogStore;

    /// <summary>
    /// Inserts a schedule in the system
    /// </summary>
    /// <param name="scheduleName">The unique schedule name</param>
    /// <param name="scheduleRecurrenceType">The type of recurrence</param>
    /// <param name="startDate">The schedule start date in UTC. If left null, the current date will be used</param>
    /// <param name="endDate">The schedule end date in UTC. If null, then no end date</param>
    /// <param name="everyX">Depending on the scheduleRecurrenceType, will mean every X hours/days/weeks/months (IE: every 3 days)</param>
    /// <param name="weekdays">The optional list of weekdays. Can only be used if the scheduleRecurrenceType is weekly</param>
    /// <param name="dayOfTheMonths">The optional list of days. Accepts [1-31] and 'L' for the last day of the month. Can only be used if the scheduleRecurrenceType is monthly</param>
    /// <param name="autoRestartOnFailure">If set to true, will reset the schedule status to ready after a failure (reached the retry limit). Otherwise, the schedule will stay in a failed state until manually started</param>
    /// <param name="retryLimit">Configure the amount of retries allowed. 0 for no retry, 10 for max retries. Retries are following a linear mechanism of 1min * retry count</param>
    /// <param name="expression">The function to call, including the parameters</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The inserted schedule</returns>
    /// <exception cref="ArgumentException">Will be thrown if the expression is not a method call, or if other configuration are invalid</exception>
    /// <exception cref="InvalidOperationException">Will be thrown if the expression passed cannot be compiled</exception>
    /// <exception cref="KeyNotFoundException">Will be thrown if the schedule could not be properly inserted and returned as null through the store</exception>
    /// <exception cref="ArgumentOutOfRangeException">Will be thrown if the date configurations are invalid</exception>
    /// <exception cref="DuplicateNameException">Will be thrown if the schedule name already exist</exception>
    Task<Schedule> InsertScheduleAsync<TJobLogStore, TItem>(string scheduleName, ScheduleRecurrenceTypes scheduleRecurrenceType, DateTime? startDate, DateTime? endDate, short everyX, IEnumerable<Weekdays>? weekdays, IEnumerable<string>? dayOfTheMonths
        , bool autoRestartOnFailure , byte retryLimit, [InstantHandle] Expression<Func<TItem, Task<JobExecutionResult>>> expression, CancellationToken cancellationToken)
        where TJobLogStore : class, IJobLogStore;

    #endregion Insert Schedule

    #region Insert Delayed Schedule

    /// <summary>
    /// Inserts a delayed schedule in the system
    /// </summary>
    /// <param name="delay">The amount of time added to the current date before the schedule is processed</param>
    /// <param name="expression">The function to call, including the parameters</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The inserted schedule</returns>
    /// <exception cref="ArgumentException">Will be thrown if the expression is not a method call, or if other configuration are invalid</exception>
    /// <exception cref="InvalidOperationException">Will be thrown if the expression passed cannot be compiled</exception>
    /// <exception cref="KeyNotFoundException">Will be thrown if the schedule could not be properly inserted and returned as null through the store</exception>
    Task<Schedule> InsertDelayedScheduleAsync<TJobLogStore>(TimeSpan delay, [InstantHandle] Expression<Func<JobExecutionResult>> expression, CancellationToken cancellationToken) where TJobLogStore : class, IJobLogStore;

    /// <summary>
    /// Inserts a delayed schedule in the system
    /// </summary>
    /// <param name="delay">The amount of time added to the current date before the schedule is processed</param>
    /// <param name="expression">The function to call, including the parameters</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The inserted schedule</returns>
    /// <exception cref="ArgumentException">Will be thrown if the expression is not a method call, or if other configuration are invalid</exception>
    /// <exception cref="InvalidOperationException">Will be thrown if the expression passed cannot be compiled</exception>
    /// <exception cref="KeyNotFoundException">Will be thrown if the schedule could not be properly inserted and returned as null through the store</exception>
    Task<Schedule> InsertDelayedScheduleAsync<TJobLogStore>(TimeSpan delay, [InstantHandle] Expression<Func<Task<JobExecutionResult>>> expression, CancellationToken cancellationToken) where TJobLogStore : class, IJobLogStore;

    /// <summary>
    /// Inserts a delayed schedule in the system
    /// </summary>
    /// <param name="delay">The amount of time added to the current date before the schedule is processed</param>
    /// <param name="expression">The function to call, including the parameters</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The inserted schedule</returns>
    /// <exception cref="ArgumentException">Will be thrown if the expression is not a method call, or if other configuration are invalid</exception>
    /// <exception cref="InvalidOperationException">Will be thrown if the expression passed cannot be compiled</exception>
    /// <exception cref="KeyNotFoundException">Will be thrown if the schedule could not be properly inserted and returned as null through the store</exception>
    Task<Schedule> InsertDelayedScheduleAsync<TJobLogStore, TItem>(TimeSpan delay, [InstantHandle] Expression<Func<TItem, JobExecutionResult>> expression, CancellationToken cancellationToken) where TJobLogStore : class, IJobLogStore;

    /// <summary>
    /// Inserts a delayed schedule in the system
    /// </summary>
    /// <param name="delay">The amount of time added to the current date before the schedule is processed</param>
    /// <param name="expression">The function to call, including the parameters</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The inserted schedule</returns>
    /// <exception cref="ArgumentException">Will be thrown if the expression is not a method call, or if other configuration are invalid</exception>
    /// <exception cref="InvalidOperationException">Will be thrown if the expression passed cannot be compiled</exception>
    /// <exception cref="KeyNotFoundException">Will be thrown if the schedule could not be properly inserted and returned as null through the store</exception>
    Task<Schedule> InsertDelayedScheduleAsync<TJobLogStore, TItem>(TimeSpan delay, [InstantHandle] Expression<Func<TItem, Task<JobExecutionResult>>> expression, CancellationToken cancellationToken) where TJobLogStore : class, IJobLogStore;

    #endregion Insert Delayed Schedule

    #endregion Insert

    #region Run

    /// <summary>
    /// Manually execute the schedule. The schedule will be added to the queue to be executed. The schedule next occurrence date will not be re-calculated to not break the normal schedule
    /// </summary>
    /// <param name="scheduleId">The schedule ID</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>Returns a job to indicate that it was properly added to the queue</returns>
    Task<Job> RunScheduleAsync(Guid scheduleId, CancellationToken cancellationToken);

    #endregion Run

    #region Update

    /// <summary>
    /// Updates the schedule name
    /// </summary>
    /// <param name="scheduleId">The schedule ID</param>
    /// <param name="scheduleName">The new unique schedule name</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>Returns the updated schedule</returns>
    /// <exception cref="InvalidOperationException">Will be thrown if the schedule is a system schedule and cannot be modified</exception>
    /// <exception cref="DuplicateNameException">Will be thrown if the schedule name already exist</exception>
    /// <exception cref="KeyNotFoundException">Will be thrown if the schedule could not be properly updated and returned as null through the store</exception>
    Task<Schedule> UpdateScheduleNameAsync(Guid scheduleId, string scheduleName, CancellationToken cancellationToken);

    /// <summary>
    /// Updates a schedule status
    /// </summary>
    /// <param name="scheduleId">The schedule ID</param>
    /// <param name="isActive">The schedule status (active/inactive)</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>Returns the updated schedule</returns>
    /// <exception cref="InvalidOperationException">Will be thrown if the schedule is a system schedule and cannot be modified</exception>
    /// <exception cref="KeyNotFoundException">Will be thrown if the schedule could not be properly updated and returned as null through the store</exception>
    Task<Schedule> UpdateScheduleStatusAsync(Guid scheduleId, bool isActive, CancellationToken cancellationToken);

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
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The updated schedule</returns>
    /// <exception cref="ArgumentException">Will be thrown if the expression is not a method call, or if other configuration are invalid</exception>
    /// <exception cref="InvalidOperationException">Will be thrown if the schedule is a system schedule and cannot be modified</exception>
    /// <exception cref="KeyNotFoundException">Will be thrown if the schedule could not be properly updated and returned as null through the store</exception>
    /// <exception cref="ArgumentOutOfRangeException">Will be thrown if the date configurations are invalid</exception>
    Task<Schedule> UpdateScheduleRecurrenceConfigsAsync(Guid scheduleId, ScheduleRecurrenceTypes scheduleRecurrenceType, DateTime? startDate, DateTime? endDate, short everyX, IReadOnlyCollection<Weekdays>? weekdays
        , IReadOnlyCollection<string>? dayOfTheMonths, CancellationToken cancellationToken);

    /// <summary>
    /// Update the schedule retry configurations
    /// </summary>
    /// <param name="scheduleId">The schedule ID</param>
    /// <param name="autoRestartOnFailure">If set to true, will reset the schedule status to ready after a failure (reached the retry limit). Otherwise, the schedule will stay in a failed state until manually started</param>
    /// <param name="retryLimit">Configure the amount of retries allowed. 0 for no retry, 10 for max retries. Retries are following a linear mechanism of 1min * retry count</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <returns>The updated schedule</returns>
    /// <exception cref="KeyNotFoundException">Will be thrown if the schedule could not be properly updated and returned as null through the store</exception>
    /// <exception cref="ArgumentException">Will be thrown if the retry configurations are invalid</exception>
    Task<Schedule> UpdateScheduleRetryConfigsAsync(Guid scheduleId, bool autoRestartOnFailure, byte retryLimit, CancellationToken cancellationToken);

    #endregion Update

    #region Delete

    /// <summary>
    /// Delete schedules by ID
    /// </summary>
    /// <param name="scheduleIds">The list of schedule IDs</param>
    /// <param name="cancellationToken">The cancellation token. Use to handle possible cancellation of the operation in progress</param>
    /// <exception cref="InvalidOperationException">Will be thrown if the schedule is a system schedule and cannot be deleted</exception>
    /// <exception cref="KeyNotFoundException">Will be thrown if one or more schedules could not be found and returned as null through the store</exception>
    Task DeleteSchedulesAsync(IReadOnlyCollection<Guid> scheduleIds, CancellationToken cancellationToken);

    #endregion Delete
}