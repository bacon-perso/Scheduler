using Bacon.Scheduler.Interfaces.Services.Batches;
using Bacon.Scheduler.Interfaces.Services.Schedules;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Models.Schedules;

namespace Bacon.Scheduler.Samples.Services.Stores;

public class InMemoryScheduleStore(InMemoryStores inMemoryStores, IBatchStore batchStore) : IScheduleStore
{
    #region Get

    public ValueTask<Schedule?> GetScheduleAsync(Guid scheduleId, CancellationToken cancellationToken)
    {
        Schedule? schedule = null;
        if (inMemoryStores.SchedulesDic.TryGetValue(scheduleId, out SchedulerWrapper? schedulerWrapper))
        {
            schedule = schedulerWrapper.Schedule;
        }

        return ValueTask.FromResult(schedule);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "<Pending>")]
    public ValueTask<IDictionary<Guid, Schedule?>> GetSchedulesAsync(IEnumerable<Guid> scheduleIds, CancellationToken cancellationToken)
    {
        IDictionary<Guid, Schedule?> schedules = new Dictionary<Guid, Schedule?>();
        foreach (Guid scheduleId in scheduleIds)
        {
            inMemoryStores.SchedulesDic.TryGetValue(scheduleId, out SchedulerWrapper? schedulerWrapper);

            schedules.Add(scheduleId, schedulerWrapper?.Schedule);
        }

        return ValueTask.FromResult(schedules);
    }

    public ValueTask<IEnumerable<Schedule>> GetAllSchedulesAsync(CancellationToken cancellationToken)
    {
        Schedule[] schedule = [.. inMemoryStores.SchedulesDic.Values.Select(s => s.Schedule)];

        return ValueTask.FromResult(schedule.AsEnumerable());
    }

    public ValueTask<JobExecutionMetadata?> GetScheduleJobMetadataAsync(Guid scheduleId, CancellationToken cancellationToken)
    {
        JobExecutionMetadata? jobExecutionMetadata = null;
        if (inMemoryStores.SchedulesDic.TryGetValue(scheduleId, out SchedulerWrapper? schedulerWrapper))
        {
            jobExecutionMetadata = schedulerWrapper.JobExecutionMetadata;
        }

        return ValueTask.FromResult(jobExecutionMetadata);
    }

    public ValueTask<IEnumerable<SchedulerWrapper>> GetSchedulesToRunAsync(DateTime currentDate, CancellationToken cancellationToken)
    {
        IEnumerable<SchedulerWrapper> schedulerWrappers = inMemoryStores.SchedulesDic.Where(w => (w.Value.Schedule.ScheduleExecutionStatus.Equals(ScheduleExecutionStatuses.Ready) && (w.Value.Schedule.NextOccurrenceDate == null || w.Value.Schedule.NextOccurrenceDate <= currentDate))
            || (w.Value.Schedule.ScheduleExecutionStatus.Equals(ScheduleExecutionStatuses.Pending) && w.Value.Schedule.ScheduleRetryConfig.NextRetryDate <= currentDate))
            .Select(s => s.Value);

        return ValueTask.FromResult(schedulerWrappers);
    }

    public ValueTask<bool> CheckScheduleNameExistsAsync(Guid? scheduleId, string scheduleName, CancellationToken cancellationToken)
    {
        Schedule? schedule = inMemoryStores.SchedulesDic.FirstOrDefault(fd => fd.Value.Schedule.ScheduleName.Equals(scheduleName, StringComparison.OrdinalIgnoreCase)).Value?.Schedule;

        bool exists = true;
        if (schedule == null)
        {
            exists = false;
        }
        else if (schedule != null && scheduleId != null && schedule.ScheduleId.Equals(scheduleId))
        {
            exists = false;
        }

        return ValueTask.FromResult(exists);
    }

    #endregion Get

    #region Insert

    public ValueTask<SchedulerWrapper?> InsertScheduleAsync(SchedulerWrapper schedulerWrapper, CancellationToken cancellationToken)
    {
        SchedulerWrapper? returnedScheduleWrapper = null;
        if (inMemoryStores.SchedulesDic.TryAdd(schedulerWrapper.Schedule.ScheduleId, schedulerWrapper))
        {
            returnedScheduleWrapper = schedulerWrapper;
        }

        return ValueTask.FromResult(returnedScheduleWrapper);
    }

    #endregion Insert

    #region Update

    public ValueTask<Schedule?> UpdateScheduleNameAsync(Guid scheduleId, string scheduleName, DateTime currentDate, CancellationToken cancellationToken)
    {
        inMemoryStores.SchedulesDic.TryGetValue(scheduleId, out SchedulerWrapper? schedulerWrapper);

        schedulerWrapper?.Schedule.ScheduleName = scheduleName;
        schedulerWrapper?.Schedule.ModificationDate = currentDate;

        return ValueTask.FromResult(schedulerWrapper?.Schedule);
    }

    public ValueTask<Schedule?> UpdateScheduleStatusAsync(Guid scheduleId, bool isActive, DateTime? nextScheduleOccurrenceDate, DateTime currentDate, CancellationToken cancellationToken)
    {
        inMemoryStores.SchedulesDic.TryGetValue(scheduleId, out SchedulerWrapper? schedulerWrapper);

        schedulerWrapper?.Schedule.IsActive = isActive;
        schedulerWrapper?.Schedule.NextOccurrenceDate = nextScheduleOccurrenceDate;
        schedulerWrapper?.Schedule.ModificationDate = currentDate;

        return ValueTask.FromResult(schedulerWrapper?.Schedule);
    }

    public ValueTask<Schedule?> UpdateScheduleRecurrenceConfigsAsync(Guid scheduleId, ScheduleRecurrenceTypes scheduleRecurrenceType, DateTime startDate, DateTime? endDate, short everyX, IEnumerable<Weekdays>? weekdays, IEnumerable<string>? dayOfTheMonths
        , DateTime? nextScheduleOccurrenceDate, DateTime currentDate, CancellationToken cancellationToken)
    {
        inMemoryStores.SchedulesDic.TryGetValue(scheduleId, out SchedulerWrapper? schedulerWrapper);

        schedulerWrapper?.Schedule.ScheduleRecurrenceConfig.ScheduleRecurrenceType = scheduleRecurrenceType;
        schedulerWrapper?.Schedule.ScheduleRecurrenceConfig.StartDate = startDate;
        schedulerWrapper?.Schedule.ScheduleRecurrenceConfig.EndDate = endDate;
        schedulerWrapper?.Schedule.ScheduleRecurrenceConfig.EveryX = everyX;
        schedulerWrapper?.Schedule.ScheduleRecurrenceConfig.Weekdays = weekdays;
        schedulerWrapper?.Schedule.ScheduleRecurrenceConfig.DaysOfTheMonth = dayOfTheMonths;
        schedulerWrapper?.Schedule.NextOccurrenceDate = nextScheduleOccurrenceDate;
        schedulerWrapper?.Schedule.ModificationDate = currentDate;

        return ValueTask.FromResult(schedulerWrapper?.Schedule);
    }

    public ValueTask<Schedule?> UpdateScheduleRetryConfigsAsync(Guid scheduleId, bool autoRestartOnFailure, byte retryLimit, DateTime currentDate, CancellationToken cancellationToken)
    {
        inMemoryStores.SchedulesDic.TryGetValue(scheduleId, out SchedulerWrapper? schedulerWrapper);

        schedulerWrapper?.Schedule.ScheduleRetryConfig.AutoRestartOnFailure = autoRestartOnFailure;
        schedulerWrapper?.Schedule.ScheduleRetryConfig.RetryLimit = retryLimit;
        schedulerWrapper?.Schedule.ModificationDate = currentDate;

        return ValueTask.FromResult(schedulerWrapper?.Schedule);
    }

    public ValueTask<IDictionary<Guid, JobExecutionMetadata?>> UpdateScheduleJobExecutionMetadataAsync(IDictionary<Guid, JobExecutionMetadata> scheduleJobExecutionMetadatas, CancellationToken cancellationToken)
    {
        IDictionary<Guid, JobExecutionMetadata?> schedules = new Dictionary<Guid, JobExecutionMetadata?>();
        foreach (KeyValuePair<Guid, JobExecutionMetadata> keyValuePair in scheduleJobExecutionMetadatas)
        {
            inMemoryStores.SchedulesDic.TryGetValue(keyValuePair.Key, out SchedulerWrapper? schedulerWrapper);

            schedulerWrapper?.JobExecutionMetadata = keyValuePair.Value;

            schedules.Add(new(keyValuePair.Key, schedulerWrapper?.JobExecutionMetadata));
        }

        return ValueTask.FromResult(schedules);
    }

    #region Internals

    public async ValueTask<Schedule?> UpdateScheduleExecutionStatusAsync(Guid scheduleId, ScheduleExecutionStatuses scheduleExecutionStatus, DateTime currentDate, CancellationToken cancellationToken)
    {
        IDictionary<Guid, Schedule?> schedules = await UpdateSchedulesExecutionStatusAsync([scheduleId], scheduleExecutionStatus, currentDate, cancellationToken);

        schedules.TryGetValue(scheduleId, out Schedule? schedule);

        return schedule;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "<Pending>")]
    public ValueTask<IDictionary<Guid, Schedule?>> UpdateSchedulesExecutionStatusAsync(IEnumerable<Guid> scheduleIds, ScheduleExecutionStatuses scheduleExecutionStatus, DateTime currentDate, CancellationToken cancellationToken)
    {
        IDictionary<Guid, Schedule?> schedules = new Dictionary<Guid, Schedule?>();
        foreach (Guid scheduleId in scheduleIds)
        {
            if (!inMemoryStores.SchedulesDic.TryGetValue(scheduleId, out SchedulerWrapper? schedulerWrapper))
            {
                schedules.Add(scheduleId, null);

                continue;
            }

            schedulerWrapper.Schedule.ScheduleExecutionStatus = scheduleExecutionStatus;
            schedulerWrapper.Schedule.ModificationDate = currentDate;

            schedules.Add(scheduleId, schedulerWrapper.Schedule);
        }

        return ValueTask.FromResult(schedules);
    }

    public ValueTask<Schedule?> UpdateScheduleRetryCountAsync(Guid scheduleId, byte scheduleRetryCount, DateTime? nextRetryDate, DateTime currentDate, CancellationToken cancellationToken)
    {
        inMemoryStores.SchedulesDic.TryGetValue(scheduleId, out SchedulerWrapper? schedulerWrapper);

        schedulerWrapper?.Schedule.ScheduleRetryConfig.RetryCount = scheduleRetryCount;
        schedulerWrapper?.Schedule.ScheduleRetryConfig.NextRetryDate = nextRetryDate;
        schedulerWrapper?.Schedule.ModificationDate = currentDate;

        return ValueTask.FromResult(schedulerWrapper?.Schedule);
    }

    public ValueTask<Schedule?> UpdateScheduleNextOccurrenceDateAsync(Guid scheduleId, DateTime? nextScheduleOccurrenceDate, DateTime currentDate, CancellationToken cancellationToken)
    {
        inMemoryStores.SchedulesDic.TryGetValue(scheduleId, out SchedulerWrapper? schedulerWrapper);

        schedulerWrapper?.Schedule.NextOccurrenceDate = nextScheduleOccurrenceDate;
        schedulerWrapper?.Schedule.ModificationDate = currentDate;

        return ValueTask.FromResult(schedulerWrapper?.Schedule);
    }

    public ValueTask<Schedule?> UpdateScheduleLastRunStatusAndDateAsync(Guid scheduleId, ScheduleOccurrenceStatuses lastScheduleOccurrenceStatus, DateTime lastScheduleOccurrenceDate, DateTime currentDate, CancellationToken cancellationToken)
    {
        inMemoryStores.SchedulesDic.TryGetValue(scheduleId, out SchedulerWrapper? schedulerWrapper);

        schedulerWrapper?.Schedule.LastOccurrenceStatus = lastScheduleOccurrenceStatus;
        schedulerWrapper?.Schedule.LastOccurrenceDate = lastScheduleOccurrenceDate;
        schedulerWrapper?.Schedule.ModificationDate = currentDate;

        return ValueTask.FromResult(schedulerWrapper?.Schedule);
    }

    public ValueTask<Schedule?> UpdateScheduleCurrentRunningBatchIdAsync(Guid scheduleId, Guid? batchId, DateTime currentDate, CancellationToken cancellationToken)
    {
        inMemoryStores.SchedulesDic.TryGetValue(scheduleId, out SchedulerWrapper? schedulerWrapper);

        schedulerWrapper?.Schedule.ScheduleRetryConfig.CurrentRunningBatchId = batchId;
        schedulerWrapper?.Schedule.ModificationDate = currentDate;

        return ValueTask.FromResult(schedulerWrapper?.Schedule);
    }

    #endregion Internals

    #endregion Update

    #region Delete

    /// <summary>
    /// Delete schedules and all related items
    /// </summary>
    /// <param name="scheduleIds"></param>
    /// <returns></returns>
    public async ValueTask DeleteSchedulesAsync(IEnumerable<Guid> scheduleIds, CancellationToken cancellationToken)
    {
        await batchStore.DeleteBatchesByScheduleIdsAsync(scheduleIds, cancellationToken);

        foreach (Guid scheduleId in scheduleIds)
        {
            inMemoryStores.SchedulesDic.TryRemove(scheduleId, out _);
        }
    }

    #endregion Delete
}