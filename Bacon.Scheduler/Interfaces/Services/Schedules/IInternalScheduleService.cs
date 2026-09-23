using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Schedules;
using System.Linq.Expressions;

namespace Bacon.Scheduler.Interfaces.Services.Schedules;

internal interface IInternalScheduleService
{
    #region Get

    Task<IEnumerable<SchedulerWrapper>> GetScheduleToRunAsync(DateTime currentDate, CancellationToken cancellationToken);

    Task<IEnumerable<Schedule>> GetAllSchedulesAsync(CancellationToken cancellationToken);

    #endregion Get

    #region Insert

    Task<Schedule> InsertScheduleAsync(string scheduleName, bool isSystem, ScheduleRecurrenceTypes scheduleRecurrenceType, DateTime startDate, DateTime? endDate, short everyX, IEnumerable<Weekdays>? weekdays, IEnumerable<string>? dayOfTheMonths
        , bool autoRestartOnFailure, byte retryLimit, LambdaExpression methodCall, Type? explicitMethodType, Type jobLogStoreImplementationType, DateTime currentDate, CancellationToken cancellationToken);

    #endregion Insert

    #region Update

    Task<Schedule> UpdateScheduleNameAsync(Guid scheduleId, string scheduleName, DateTime currentDate, CancellationToken cancellationToken);

    Task<Schedule> UpdateScheduleStatusAsync(Schedule schedule, bool isActive, DateTime currentDate, CancellationToken cancellationToken);

    Task<Schedule> UpdateScheduleRecurrenceConfigsAsync(Guid scheduleId, DateTime? scheduleNextOccurrenceDate, ScheduleRecurrenceTypes scheduleRecurrenceType, DateTime startDate, DateTime? endDate, short everyX, IReadOnlyCollection<Weekdays>? weekdays
        , IReadOnlyCollection<string>? dayOfTheMonths, DateTime currentDate, CancellationToken cancellationToken);

    Task<Schedule> UpdateScheduleRetryConfigsAsync(Guid scheduleId, bool autoRestartOnFailure, byte retryLimit, DateTime currentDate, CancellationToken cancellationToken);

    Task UpdateScheduleJobExecutionMetadataAsync(IDictionary<Guid, (LambdaExpression methodCall, Type? explicitType, Type jobLogStoreImplementationType)> scheduleJobExecutionMetadatas, DateTime currentTime, CancellationToken cancellationToken);

    #region Internal mechanism

    Task<Schedule> UpdateScheduleExecutionStatusAsync(Guid scheduleId, ScheduleExecutionStatuses scheduleExecutionStatus, DateTime currentDate, CancellationToken cancellationToken);

    Task<IEnumerable<Schedule>> UpdateSchedulesExecutionStatusAsync(IReadOnlyCollection<Guid> scheduleIds, ScheduleExecutionStatuses scheduleExecutionStatus, DateTime currentDate, CancellationToken cancellationToken);

    Task<Schedule> UpdateScheduleRetryCountAsync(Guid scheduleId, byte retryCount, DateTime currentDate, CancellationToken cancellationToken);

    Task<Schedule> UpdateScheduleNextOccurrenceDateAsync(Schedule schedule, DateTime currentDate, CancellationToken cancellationToken);

    Task<Schedule> UpdateScheduleLastRunStatusAndDateAsync(Guid scheduleId, ScheduleOccurrenceStatuses lastScheduleOccurrenceStatus, DateTime lastScheduleOccurrenceDate, DateTime currentDate, CancellationToken cancellationToken);

    Task<Schedule> UpdateScheduleCurrentRunningBatchIdAsync(Guid scheduleId, Guid? batchId, DateTime currentDate, CancellationToken cancellationToken);

    #endregion Internal mechanism

    #endregion Update

    #region Delete

    Task DeleteSchedulesAsync(IReadOnlyCollection<Guid> scheduleIds, CancellationToken cancellationToken);

    #endregion Delete
}