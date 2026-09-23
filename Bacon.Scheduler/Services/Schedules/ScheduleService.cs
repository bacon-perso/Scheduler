using Bacon.Scheduler.Extensions;
using Bacon.Scheduler.Interfaces.Services.Batches;
using Bacon.Scheduler.Interfaces.Services.Jobs;
using Bacon.Scheduler.Interfaces.Services.Jobs.JobLogs;
using Bacon.Scheduler.Interfaces.Services.Queues;
using Bacon.Scheduler.Interfaces.Services.Schedules;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Batches;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Models.Queues;
using Bacon.Scheduler.Models.Schedules;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Bacon.Scheduler.Services.Schedules;

internal sealed class ScheduleService(IScheduleStore scheduleStore, IScheduleValidationService scheduleValidationService, IInternalScheduleService internalScheduleService, IBatchService batchService, IJobService jobService, IQueueHandlerService queueHandlerService
    , IOptions<SchedulerOptions> schedulerOptionsOptions, TimeProvider timeProvider) : IScheduleService
{
    #region Get

    public async Task<Schedule> GetScheduleAsync(Guid scheduleId, CancellationToken cancellationToken)
    {
        Schedule? schedule = await scheduleStore.GetScheduleAsync(scheduleId, cancellationToken).ConfigureAwait(false);

        return schedule ?? throw new KeyNotFoundException($"Scheduler - The schedule '{scheduleId}' cannot be found");
    }

    public async Task<IEnumerable<Schedule>> GetSchedulesAsync(IReadOnlyCollection<Guid> scheduleIds, CancellationToken cancellationToken)
    {
        IDictionary<Guid, Schedule?> schedulesDic = await scheduleStore.GetSchedulesAsync(scheduleIds, cancellationToken).ConfigureAwait(false);

        (IReadOnlyCollection<Schedule> found, IReadOnlyCollection<Guid> missing) = schedulesDic.PartitionLookupData(scheduleIds);

        if (missing.Count != 0)
        {
            throw new KeyNotFoundException($"Scheduler - The following schedules cannot be found: '{string.Join(", ", missing)}'");
        }

        return found;
    }

    #endregion Get

    #region Insert

    #region Insert Schedule

    public async Task<Schedule> InsertScheduleAsync<TJobLogStore>(string scheduleName, ScheduleRecurrenceTypes scheduleRecurrenceType, DateTime? startDate, DateTime? endDate, short everyX, IEnumerable<Weekdays>? weekdays, IEnumerable<string>? dayOfTheMonths, bool autoRestartOnFailure
        , byte retryLimit, [InstantHandle] Expression<Func<Task<JobExecutionResult>>> expression, CancellationToken cancellationToken) where TJobLogStore : class, IJobLogStore
    {
        return await InsertScheduleInternalAsync<TJobLogStore>(
            scheduleName,
            scheduleRecurrenceType,
            startDate,
            endDate,
            everyX,
            weekdays?.ToArray(),
            dayOfTheMonths?.ToArray(),
            autoRestartOnFailure,
            retryLimit,
            expression,
            null,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<Schedule> InsertScheduleAsync<TJobLogStore>(string scheduleName, ScheduleRecurrenceTypes scheduleRecurrenceType, DateTime? startDate, DateTime? endDate, short everyX, IEnumerable<Weekdays>? weekdays, IEnumerable<string>? dayOfTheMonths, bool autoRestartOnFailure
        , byte retryLimit, [InstantHandle] Expression<Func<JobExecutionResult>> expression, CancellationToken cancellationToken) where TJobLogStore : class, IJobLogStore
    {
        return await InsertScheduleInternalAsync<TJobLogStore>(
            scheduleName,
            scheduleRecurrenceType,
            startDate,
            endDate,
            everyX,
            weekdays?.ToArray(),
            dayOfTheMonths?.ToArray(),
            autoRestartOnFailure,
            retryLimit,
            expression,
            null,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<Schedule> InsertScheduleAsync<TJobLogStore, TItem>(string scheduleName, ScheduleRecurrenceTypes scheduleRecurrenceType, DateTime? startDate, DateTime? endDate, short everyX, IEnumerable<Weekdays>? weekdays, IEnumerable<string>? dayOfTheMonths, bool autoRestartOnFailure
        , byte retryLimit, [InstantHandle] Expression<Func<TItem, JobExecutionResult>> expression, CancellationToken cancellationToken) where TJobLogStore : class, IJobLogStore
    {
        return await InsertScheduleInternalAsync<TJobLogStore>(
            scheduleName,
            scheduleRecurrenceType,
            startDate,
            endDate,
            everyX,
            weekdays?.ToArray(),
            dayOfTheMonths?.ToArray(),
            autoRestartOnFailure,
            retryLimit,
            expression,
            typeof(TItem),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<Schedule> InsertScheduleAsync<TJobLogStore, TItem>(string scheduleName, ScheduleRecurrenceTypes scheduleRecurrenceType, DateTime? startDate, DateTime? endDate, short everyX, IEnumerable<Weekdays>? weekdays, IEnumerable<string>? dayOfTheMonths, bool autoRestartOnFailure
        , byte retryLimit, [InstantHandle] Expression<Func<TItem, Task<JobExecutionResult>>> expression, CancellationToken cancellationToken) where TJobLogStore : class, IJobLogStore
    {
        return await InsertScheduleInternalAsync<TJobLogStore>(
            scheduleName,
            scheduleRecurrenceType,
            startDate,
            endDate,
            everyX,
            weekdays?.ToArray(),
            dayOfTheMonths?.ToArray(),
            autoRestartOnFailure,
            retryLimit,
            expression,
            typeof(TItem),
            cancellationToken).ConfigureAwait(false);
    }

    #endregion Insert Schedule

    #region Insert Delayed Schedule

    public async Task<Schedule> InsertDelayedScheduleAsync<TJobLogStore>(TimeSpan delay, [InstantHandle] Expression<Func<JobExecutionResult>> expression, CancellationToken cancellationToken) where TJobLogStore : class, IJobLogStore
    {
        return await InsertDelayedScheduleAsync<TJobLogStore>(delay, expression, null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Schedule> InsertDelayedScheduleAsync<TJobLogStore>(TimeSpan delay, [InstantHandle] Expression<Func<Task<JobExecutionResult>>> expression, CancellationToken cancellationToken) where TJobLogStore : class, IJobLogStore
    {
        return await InsertDelayedScheduleAsync<TJobLogStore>(delay, expression, null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Schedule> InsertDelayedScheduleAsync<TJobLogStore, TItem>(TimeSpan delay, [InstantHandle] Expression<Func<TItem, JobExecutionResult>> expression, CancellationToken cancellationToken) where TJobLogStore : class, IJobLogStore
    {
        return await InsertDelayedScheduleAsync<TJobLogStore>(delay, expression, typeof(TItem), cancellationToken).ConfigureAwait(false);
    }

    public async Task<Schedule> InsertDelayedScheduleAsync<TJobLogStore, TItem>(TimeSpan delay, [InstantHandle] Expression<Func<TItem, Task<JobExecutionResult>>> expression, CancellationToken cancellationToken) where TJobLogStore : class, IJobLogStore
    {
        return await InsertDelayedScheduleAsync<TJobLogStore>(delay, expression, typeof(TItem), cancellationToken).ConfigureAwait(false);
    }

    #endregion Insert Delayed Schedule

    #endregion Insert

    #region Run

    public async Task<Job> RunScheduleAsync(Guid scheduleId, CancellationToken cancellationToken)
    {
        #region Validations

        Schedule schedule = await GetScheduleAsync(scheduleId, cancellationToken).ConfigureAwait(false);

        if (!schedule.IsActive)
        {
            throw new InvalidOperationException("Scheduler - The schedule is inactive and cannot be executed");
        }

        if (!schedulerOptionsOptions.Value.AllowSystemScheduleManualExecution && schedule.IsSystem)
        {
            throw new InvalidOperationException("Scheduler - The scheduler does not allow system schedule to be manually executed");
        }

        JobExecutionMetadata? jobExecutionMetadata = await scheduleStore.GetScheduleJobMetadataAsync(scheduleId, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("Scheduler - The schedule does not have a job execution metadata");

        #endregion Validations

        DateTime currentDate = timeProvider.GetUtcNow().UtcDateTime;

        Batch batch = await batchService.InsertBatchAsync(schedule, currentDate, cancellationToken).ConfigureAwait(false);

        Job job = await jobService.InsertJobAsync(batch, cancellationToken).ConfigureAwait(false);

        ScheduleQueueItemPayload scheduleQueueItemPayload = new()
        {
            JobExecutionOrigin = JobExecutionOrigins.Manual,
            Job = job,
            JobExecutionMetadata = jobExecutionMetadata,
            Schedule = schedule
        };

        await queueHandlerService.EnqueueAsync(scheduleQueueItemPayload, cancellationToken).ConfigureAwait(false);

        return job;
    }

    #endregion Run

    #region Update

    public async Task<Schedule> UpdateScheduleNameAsync(Guid scheduleId, string scheduleName, CancellationToken cancellationToken)
    {
        #region Validations

        Schedule schedule = await GetScheduleAsync(scheduleId, cancellationToken).ConfigureAwait(false);

        scheduleValidationService.ValidateSystemSchedules([schedule]);

        await scheduleValidationService.ValidateScheduleNameExist(scheduleId, scheduleName, cancellationToken).ConfigureAwait(false);

        #endregion Validations

        return await internalScheduleService.UpdateScheduleNameAsync(scheduleId, scheduleName, timeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Schedule> UpdateScheduleStatusAsync(Guid scheduleId, bool isActive, CancellationToken cancellationToken)
    {
        #region Validations

        Schedule schedule = await GetScheduleAsync(scheduleId, cancellationToken).ConfigureAwait(false);

        scheduleValidationService.ValidateSystemSchedules([schedule]);

        #endregion Validations

        return await internalScheduleService.UpdateScheduleStatusAsync(schedule, isActive, timeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Schedule> UpdateScheduleRecurrenceConfigsAsync(Guid scheduleId, ScheduleRecurrenceTypes scheduleRecurrenceType, DateTime? startDate, DateTime? endDate, short everyX, IReadOnlyCollection<Weekdays>? weekdays
        , IReadOnlyCollection<string>? dayOfTheMonths, CancellationToken cancellationToken)
    {
        DateTime currentDate = timeProvider.GetUtcNow().UtcDateTime;

        #region Validations

        Schedule schedule = await GetScheduleAsync(scheduleId, cancellationToken).ConfigureAwait(false);

        scheduleValidationService.ValidateSystemSchedules([schedule]);

        scheduleValidationService.ValidateUtcDates(startDate, endDate);
        scheduleValidationService.ValidateScheduleRecurrence(scheduleId.ToString(), scheduleRecurrenceType, everyX, weekdays, dayOfTheMonths);

        startDate ??= currentDate;

        scheduleValidationService.ValidateMonthlyDayOfTheMonths(scheduleId.ToString(), scheduleRecurrenceType, dayOfTheMonths);
        scheduleValidationService.ValidateImpossibleRecurrence(scheduleId.ToString(), scheduleRecurrenceType, startDate.Value, everyX, dayOfTheMonths);

        #endregion Validations

        return await internalScheduleService.UpdateScheduleRecurrenceConfigsAsync(schedule.ScheduleId, schedule.NextOccurrenceDate, scheduleRecurrenceType, startDate.Value, endDate, everyX, weekdays, dayOfTheMonths, currentDate, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Schedule> UpdateScheduleRetryConfigsAsync(Guid scheduleId, bool autoRestartOnFailure, byte retryLimit, CancellationToken cancellationToken)
    {
        #region Validations

        Schedule schedule = await GetScheduleAsync(scheduleId, cancellationToken).ConfigureAwait(false);

        scheduleValidationService.ValidateSystemSchedules([schedule]);

        scheduleValidationService.ValidateScheduleRetry(scheduleId.ToString(), retryLimit);

        #endregion Validations

        return await internalScheduleService.UpdateScheduleRetryConfigsAsync(schedule.ScheduleId, autoRestartOnFailure, retryLimit, timeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false);
    }

    #endregion Update

    #region Delete

    public async Task DeleteSchedulesAsync(IReadOnlyCollection<Guid> scheduleIds, CancellationToken cancellationToken)
    {
        #region Validations

        IEnumerable<Schedule> schedules = await GetSchedulesAsync(scheduleIds, cancellationToken).ConfigureAwait(false);

        scheduleValidationService.ValidateSystemSchedules(schedules);

        #endregion Validations

        await internalScheduleService.DeleteSchedulesAsync(scheduleIds, cancellationToken).ConfigureAwait(false);
    }

    #endregion Delete

    #region Private

    #region Insert

    #region Insert Delayed

    private async Task<Schedule> InsertDelayedScheduleAsync<TJobLogStore>(TimeSpan delay, [NotNull] LambdaExpression methodCall, Type? explicitType, CancellationToken cancellationToken) where TJobLogStore : class, IJobLogStore
    {
        #region Validations

        if (delay.Ticks <= 0 || delay.Ticks > TimeSpan.FromHours(1).Ticks)
        {
            throw new ArgumentException($"Scheduler - There was an error inserting the delayed schedule. THe delay accepts a value greater than 0 or smaller than 1 hours", nameof(delay));
        }

        #endregion Validations

        DateTime currentDate = timeProvider.GetUtcNow().UtcDateTime;
        DateTime startDate = currentDate.Add(delay);

        return await internalScheduleService.InsertScheduleAsync(
            $"DelayedSchedule-{Guid.NewGuid():N}{Guid.NewGuid():N}",
            false,
            ScheduleRecurrenceTypes.Daily,
            startDate,
            startDate.AddHours(1),
            1,
            null,
            null,
            false,
            0,
            methodCall,
            explicitType,
            typeof(TJobLogStore),
            currentDate,
            cancellationToken).ConfigureAwait(false);
    }

    #endregion Insert Delayed Schedule

    internal async Task<Schedule> InsertScheduleInternalAsync<TJobLogStore>(string scheduleName, ScheduleRecurrenceTypes scheduleRecurrenceType, DateTime? startDate, DateTime? endDate, short everyX, IReadOnlyCollection<Weekdays>? weekdays, IReadOnlyCollection<string>? dayOfTheMonths
        , bool autoRestartOnFailure, byte retryLimit, [NotNull] LambdaExpression methodCall, Type? explicitType, CancellationToken cancellationToken) where TJobLogStore : class, IJobLogStore
    {
        DateTime currentDate = timeProvider.GetUtcNow().UtcDateTime;

        #region Validations

        scheduleValidationService.ValidateScheduleRetry(scheduleName, retryLimit);
        scheduleValidationService.ValidateUtcDates(startDate, endDate);

        startDate ??= currentDate;

        scheduleValidationService.ValidateStartAndEndDate(startDate.Value, endDate, currentDate);
        scheduleValidationService.ValidateScheduleRecurrence(scheduleName, scheduleRecurrenceType, everyX, weekdays, dayOfTheMonths);
        scheduleValidationService.ValidateMonthlyDayOfTheMonths(scheduleName, scheduleRecurrenceType, dayOfTheMonths);
        scheduleValidationService.ValidateImpossibleRecurrence(scheduleName, scheduleRecurrenceType, startDate.Value, everyX, dayOfTheMonths);

        await scheduleValidationService.ValidateScheduleNameExist(null, scheduleName, cancellationToken).ConfigureAwait(false);

        #endregion Validations

        return await internalScheduleService.InsertScheduleAsync(
            scheduleName,
            false,
            scheduleRecurrenceType,
            startDate.Value,
            endDate,
            everyX,
            weekdays,
            dayOfTheMonths,
            autoRestartOnFailure,
            retryLimit,
            methodCall,
            explicitType,
            typeof(TJobLogStore),
            currentDate,
            cancellationToken).ConfigureAwait(false);
    }

    #endregion Insert

    #endregion Private
}