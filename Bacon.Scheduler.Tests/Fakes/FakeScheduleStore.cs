using Bacon.Scheduler.Interfaces.Services.Schedules;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Models.Schedules;
using System.Collections.Concurrent;

namespace Bacon.Scheduler.Tests.Fakes;

internal sealed class FakeScheduleStore : IScheduleStore
{
    public readonly ConcurrentDictionary<Guid, SchedulerWrapper> Schedules = [];

    public ValueTask<Schedule?> GetScheduleAsync(Guid scheduleId, CancellationToken cancellationToken)
    {
        Schedules.TryGetValue(scheduleId, out SchedulerWrapper? wrapper);
        return ValueTask.FromResult(wrapper?.Schedule);
    }

    public ValueTask<IDictionary<Guid, Schedule?>> GetSchedulesAsync(IEnumerable<Guid> scheduleIds, CancellationToken cancellationToken)
    {
        IDictionary<Guid, Schedule?> result = new Dictionary<Guid, Schedule?>();
        foreach (Guid scheduleId in scheduleIds)
        {
            Schedules.TryGetValue(scheduleId, out SchedulerWrapper? wrapper);
            result[scheduleId] = wrapper?.Schedule;
        }

        return ValueTask.FromResult(result);
    }

    public ValueTask<IEnumerable<Schedule>> GetAllSchedulesAsync(CancellationToken cancellationToken)
        => ValueTask.FromResult(Schedules.Values.Select(s => s.Schedule));

    public ValueTask<JobExecutionMetadata?> GetScheduleJobMetadataAsync(Guid scheduleId, CancellationToken cancellationToken)
    {
        Schedules.TryGetValue(scheduleId, out SchedulerWrapper? wrapper);
        return ValueTask.FromResult(wrapper?.JobExecutionMetadata);
    }

    public ValueTask<bool> CheckScheduleNameExistsAsync(Guid? scheduleId, string scheduleName, CancellationToken cancellationToken)
    {
        SchedulerWrapper? match = Schedules.Values.FirstOrDefault(w => w.Schedule.ScheduleName.Equals(scheduleName, StringComparison.OrdinalIgnoreCase));
        bool exists = match is not null && (scheduleId is null || !match.Schedule.ScheduleId.Equals(scheduleId.Value));
        return ValueTask.FromResult(exists);
    }

    public ValueTask<IEnumerable<SchedulerWrapper>> GetSchedulesToRunAsync(DateTime currentDate, CancellationToken cancellationToken)
    {
        IEnumerable<SchedulerWrapper> result = Schedules.Values.Where(w =>
            (w.Schedule.IsActive && w.Schedule.ScheduleExecutionStatus == ScheduleExecutionStatuses.Ready && (w.Schedule.NextOccurrenceDate == null || w.Schedule.NextOccurrenceDate <= currentDate))
            || (w.Schedule.IsActive && w.Schedule.ScheduleExecutionStatus == ScheduleExecutionStatuses.Pending && w.Schedule.ScheduleRetryConfig.NextRetryDate <= currentDate));

        return ValueTask.FromResult(result);
    }

    public ValueTask<SchedulerWrapper?> InsertScheduleAsync(SchedulerWrapper schedulerWrapper, CancellationToken cancellationToken)
    {
        bool added = Schedules.TryAdd(schedulerWrapper.Schedule.ScheduleId, schedulerWrapper);
        return ValueTask.FromResult(added ? schedulerWrapper : null);
    }

    public ValueTask<Schedule?> UpdateScheduleNameAsync(Guid scheduleId, string scheduleName, DateTime currentDate, CancellationToken cancellationToken)
    {
        if (!Schedules.TryGetValue(scheduleId, out SchedulerWrapper? wrapper))
        {
            return ValueTask.FromResult<Schedule?>(null);
        }

        wrapper.Schedule.ScheduleName = scheduleName;
        wrapper.Schedule.ModificationDate = currentDate;
        return ValueTask.FromResult<Schedule?>(wrapper.Schedule);
    }

    public ValueTask<Schedule?> UpdateScheduleStatusAsync(Guid scheduleId, bool isActive, DateTime? nextScheduleOccurrenceDate, DateTime currentDate, CancellationToken cancellationToken)
    {
        if (!Schedules.TryGetValue(scheduleId, out SchedulerWrapper? wrapper))
        {
            return ValueTask.FromResult<Schedule?>(null);
        }

        wrapper.Schedule.IsActive = isActive;
        wrapper.Schedule.NextOccurrenceDate = nextScheduleOccurrenceDate;
        wrapper.Schedule.ModificationDate = currentDate;
        return ValueTask.FromResult<Schedule?>(wrapper.Schedule);
    }

    public ValueTask<Schedule?> UpdateScheduleRecurrenceConfigsAsync(Guid scheduleId, ScheduleRecurrenceTypes scheduleRecurrenceType, DateTime startDate, DateTime? endDate, short everyX, IEnumerable<Weekdays>? weekdays, IEnumerable<string>? dayOfTheMonths
        , DateTime? nextScheduleOccurrenceDate, DateTime currentDate, CancellationToken cancellationToken)
    {
        if (!Schedules.TryGetValue(scheduleId, out SchedulerWrapper? wrapper))
        {
            return ValueTask.FromResult<Schedule?>(null);
        }

        wrapper.Schedule.ScheduleRecurrenceConfig.ScheduleRecurrenceType = scheduleRecurrenceType;
        wrapper.Schedule.ScheduleRecurrenceConfig.StartDate = startDate;
        wrapper.Schedule.ScheduleRecurrenceConfig.EndDate = endDate;
        wrapper.Schedule.ScheduleRecurrenceConfig.EveryX = everyX;
        wrapper.Schedule.ScheduleRecurrenceConfig.Weekdays = weekdays;
        wrapper.Schedule.ScheduleRecurrenceConfig.DaysOfTheMonth = dayOfTheMonths;
        wrapper.Schedule.NextOccurrenceDate = nextScheduleOccurrenceDate;
        wrapper.Schedule.ModificationDate = currentDate;
        return ValueTask.FromResult<Schedule?>(wrapper.Schedule);
    }

    public ValueTask<Schedule?> UpdateScheduleRetryConfigsAsync(Guid scheduleId, bool autoRestartOnFailure, byte retryLimit, DateTime currentDate, CancellationToken cancellationToken)
    {
        if (!Schedules.TryGetValue(scheduleId, out SchedulerWrapper? wrapper))
        {
            return ValueTask.FromResult<Schedule?>(null);
        }

        wrapper.Schedule.ScheduleRetryConfig.AutoRestartOnFailure = autoRestartOnFailure;
        wrapper.Schedule.ScheduleRetryConfig.RetryLimit = retryLimit;
        wrapper.Schedule.ModificationDate = currentDate;
        return ValueTask.FromResult<Schedule?>(wrapper.Schedule);
    }

    public ValueTask<Schedule?> UpdateScheduleExecutionStatusAsync(Guid scheduleId, ScheduleExecutionStatuses scheduleExecutionStatus, DateTime currentDate, CancellationToken cancellationToken)
    {
        if (!Schedules.TryGetValue(scheduleId, out SchedulerWrapper? wrapper))
        {
            return ValueTask.FromResult<Schedule?>(null);
        }

        wrapper.Schedule.ScheduleExecutionStatus = scheduleExecutionStatus;
        wrapper.Schedule.ModificationDate = currentDate;
        return ValueTask.FromResult<Schedule?>(wrapper.Schedule);
    }

    public ValueTask<IDictionary<Guid, JobExecutionMetadata?>> UpdateScheduleJobExecutionMetadataAsync(IDictionary<Guid, JobExecutionMetadata> scheduleJobExecutionMetadatas, CancellationToken cancellationToken)
    {
        IDictionary<Guid, JobExecutionMetadata?> result = new Dictionary<Guid, JobExecutionMetadata?>();
        foreach (KeyValuePair<Guid, JobExecutionMetadata> keyValuePair in scheduleJobExecutionMetadatas)
        {
            if (Schedules.TryGetValue(keyValuePair.Key, out SchedulerWrapper? wrapper))
            {
                wrapper.JobExecutionMetadata = keyValuePair.Value;
                result[keyValuePair.Key] = wrapper.JobExecutionMetadata;
            }
            else
            {
                result[keyValuePair.Key] = null;
            }
        }

        return ValueTask.FromResult(result);
    }

    public ValueTask<IDictionary<Guid, Schedule?>> UpdateSchedulesExecutionStatusAsync(IEnumerable<Guid> scheduleIds, ScheduleExecutionStatuses scheduleExecutionStatus, DateTime currentDate, CancellationToken cancellationToken)
    {
        IDictionary<Guid, Schedule?> result = new Dictionary<Guid, Schedule?>();
        foreach (Guid scheduleId in scheduleIds)
        {
            if (Schedules.TryGetValue(scheduleId, out SchedulerWrapper? wrapper))
            {
                wrapper.Schedule.ScheduleExecutionStatus = scheduleExecutionStatus;
                wrapper.Schedule.ModificationDate = currentDate;
                result[scheduleId] = wrapper.Schedule;
            }
            else
            {
                result[scheduleId] = null;
            }
        }

        return ValueTask.FromResult(result);
    }

    public ValueTask<Schedule?> UpdateScheduleRetryCountAsync(Guid scheduleId, byte scheduleRetryCount, DateTime? nextRetryDate, DateTime currentDate, CancellationToken cancellationToken)
    {
        if (!Schedules.TryGetValue(scheduleId, out SchedulerWrapper? wrapper))
        {
            return ValueTask.FromResult<Schedule?>(null);
        }

        wrapper.Schedule.ScheduleRetryConfig.RetryCount = scheduleRetryCount;
        wrapper.Schedule.ScheduleRetryConfig.NextRetryDate = nextRetryDate;
        wrapper.Schedule.ModificationDate = currentDate;
        return ValueTask.FromResult<Schedule?>(wrapper.Schedule);
    }

    public ValueTask<Schedule?> UpdateScheduleNextOccurrenceDateAsync(Guid scheduleId, DateTime? nextScheduleOccurrenceDate, DateTime currentDate, CancellationToken cancellationToken)
    {
        if (!Schedules.TryGetValue(scheduleId, out SchedulerWrapper? wrapper))
        {
            return ValueTask.FromResult<Schedule?>(null);
        }

        wrapper.Schedule.NextOccurrenceDate = nextScheduleOccurrenceDate;
        wrapper.Schedule.ModificationDate = currentDate;
        return ValueTask.FromResult<Schedule?>(wrapper.Schedule);
    }

    public ValueTask<Schedule?> UpdateScheduleLastRunStatusAndDateAsync(Guid scheduleId, ScheduleOccurrenceStatuses lastScheduleOccurrenceStatus, DateTime lastScheduleOccurrenceDate, DateTime currentDate, CancellationToken cancellationToken)
    {
        if (!Schedules.TryGetValue(scheduleId, out SchedulerWrapper? wrapper))
        {
            return ValueTask.FromResult<Schedule?>(null);
        }

        wrapper.Schedule.LastOccurrenceStatus = lastScheduleOccurrenceStatus;
        wrapper.Schedule.LastOccurrenceDate = lastScheduleOccurrenceDate;
        wrapper.Schedule.ModificationDate = currentDate;
        return ValueTask.FromResult<Schedule?>(wrapper.Schedule);
    }

    public ValueTask<Schedule?> UpdateScheduleCurrentRunningBatchIdAsync(Guid scheduleId, Guid? batchId, DateTime currentDate, CancellationToken cancellationToken)
    {
        if (!Schedules.TryGetValue(scheduleId, out SchedulerWrapper? wrapper))
        {
            return ValueTask.FromResult<Schedule?>(null);
        }

        wrapper.Schedule.ScheduleRetryConfig.CurrentRunningBatchId = batchId;
        wrapper.Schedule.ModificationDate = currentDate;
        return ValueTask.FromResult<Schedule?>(wrapper.Schedule);
    }

    public ValueTask DeleteSchedulesAsync(IEnumerable<Guid> scheduleIds, CancellationToken cancellationToken)
    {
        foreach (Guid scheduleId in scheduleIds)
        {
            Schedules.TryRemove(scheduleId, out _);
        }

        return ValueTask.CompletedTask;
    }
}
