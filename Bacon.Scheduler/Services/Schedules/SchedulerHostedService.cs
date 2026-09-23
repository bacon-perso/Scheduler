using Bacon.Scheduler.Interfaces.Services.Batches;
using Bacon.Scheduler.Interfaces.Services.Jobs;
using Bacon.Scheduler.Interfaces.Services.Queues;
using Bacon.Scheduler.Interfaces.Services.Schedules;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Batches;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Models.Queues;
using Bacon.Scheduler.Models.Schedules;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;
using System.Linq.Expressions;

namespace Bacon.Scheduler.Services.Schedules;

internal sealed partial class SchedulerHostedService(IQueueHandlerService queueHandlerService, IInternalScheduleService internalScheduleService, IScheduleValidationService scheduleValidationService, IBatchService batchService, IJobService jobService
    , IOptions<SchedulerOptions> schedulerOptionsOptions, Dictionary<string, SystemSchedule> systemSchedules, TimeProvider timeProvider, ILogger<SchedulerHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await InitSystemSchedules(cancellationToken);

        using PeriodicTimer timer = new(schedulerOptionsOptions.Value.PeriodicTimerInterval);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            DateTime currentDate = timeProvider.GetUtcNow().UtcDateTime;

            try
            {
                await GetSchedulesToExecute(currentDate, cancellationToken);
            }
            catch (Exception e)
            {
                LogExecuteError(currentDate, e);
            }
        }
    }

    #region Private

    #region System Schedules

    private async Task InitSystemSchedules(CancellationToken cancellationToken)
    {
        DateTime currentDate = timeProvider.GetUtcNow().UtcDateTime;

        IEnumerable<Schedule> allSchedules = await internalScheduleService.GetAllSchedulesAsync(cancellationToken);

        Dictionary<string, Schedule> schedules = allSchedules.Where(w => !w.ScheduleName.StartsWith("DelayedSchedule-", StringComparison.Ordinal)).ToDictionary(key => key.ScheduleName, value => value, StringComparer.OrdinalIgnoreCase);

        List<(SystemSchedule, Schedule?)> existingSystemSchedules = [];
        foreach (KeyValuePair<string, SystemSchedule> keyValuePair in systemSchedules)
        {
            schedules.TryGetValue(keyValuePair.Key, out Schedule? schedule);

            existingSystemSchedules.Add((keyValuePair.Value, schedule));
        }

        List<Exception> exceptions = ValidateSystemSchedules(existingSystemSchedules, currentDate);

        #region Delete

        IEnumerable<Guid> schedulesToDelete = schedules.Where(w => w.Value.IsSystem && !systemSchedules.ContainsKey(w.Key)).Select(s => s.Value.ScheduleId);
        if (schedulesToDelete.Any())
        {
            try
            {
                await internalScheduleService.DeleteSchedulesAsync([.. schedulesToDelete], cancellationToken);
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }

        #endregion Delete

        List<Task<Exception?>> tasks = [];
        foreach ((SystemSchedule systemSchedule, Schedule? existingSchedule) in existingSystemSchedules)
        {
            tasks.Add(HandleSystemSchedule(systemSchedule, existingSchedule, currentDate, cancellationToken));
        }

        await Task.WhenAll(tasks);

        foreach (Task<Exception?> task in tasks)
        {
            Exception? exception = await task.ConfigureAwait(false);

            if (exception is not null)
            {
                exceptions.Add(exception);
            }
        }

        Dictionary<Guid, (LambdaExpression methodCall, Type? jobExecutionImplementationType, Type jobLogStoreImplementationType)> scheduleJobExecutionMetadatas = [];
        foreach ((SystemSchedule systemSchedule, Schedule? existingSchedule) in existingSystemSchedules.Where(w => w.Item2 is not null))
        {
            scheduleJobExecutionMetadatas.Add(existingSchedule!.ScheduleId, (systemSchedule.MethodCall, systemSchedule.JobType, systemSchedule.JobLogStoreImplementationType));
        }

        try
        {
            await internalScheduleService.UpdateScheduleJobExecutionMetadataAsync(scheduleJobExecutionMetadatas, currentDate, cancellationToken);
        }
        catch (Exception e)
        {
            exceptions.Add(e);
        }

        if (exceptions.Count != 0)
        {
            AggregateException aggregateException = new(exceptions);

            LogInitSystemSchedulesError(aggregateException);

            throw aggregateException;
        }
    }

    #region Validate System Schedules

    private List<Exception> ValidateSystemSchedules(List<(SystemSchedule, Schedule?)> systemSchedules, DateTime currentDate)
    {
        List<Exception> exceptions = [];
        foreach ((SystemSchedule systemSchedule, Schedule? existingSchedule) in systemSchedules)
        {
            try
            {
                scheduleValidationService.ValidateScheduleRetry(systemSchedule.ScheduleName, systemSchedule.RetryLimit);
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }

            try
            {
                scheduleValidationService.ValidateScheduleRecurrence(systemSchedule.ScheduleName, systemSchedule.ScheduleRecurrenceType, systemSchedule.EveryX, systemSchedule.Weekdays, systemSchedule.DayOfTheMonths);
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }

            try
            {
                scheduleValidationService.ValidateMonthlyDayOfTheMonths(systemSchedule.ScheduleName, systemSchedule.ScheduleRecurrenceType, systemSchedule.DayOfTheMonths);
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }

            try
            {
                scheduleValidationService.ValidateImpossibleRecurrence(systemSchedule.ScheduleName, systemSchedule.ScheduleRecurrenceType, existingSchedule?.ScheduleRecurrenceConfig.StartDate ?? currentDate, systemSchedule.EveryX, systemSchedule.DayOfTheMonths);
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }

        return exceptions;
    }

    #endregion Validate System Schedules

    #region Handle System Schedule

    private async Task<Exception?> HandleSystemSchedule(SystemSchedule systemSchedule, Schedule? existingSchedule, DateTime currentDate, CancellationToken cancellationToken)
    {
        try
        {
            #region Insert

            if (existingSchedule == null)
            {
                await InsertSystemSchedule(systemSchedule, currentDate, cancellationToken);
                return null;
            }

            #endregion Insert

            #region Update

            await UpdateSystemSchedule(systemSchedule, existingSchedule, currentDate, cancellationToken);
            return null;

            #endregion Update
        }
        catch (Exception e)
        {
            return e;
        }
    }

    #endregion Handle System Schedule

    #region Insert System Schedules

    private async Task InsertSystemSchedule(SystemSchedule systemSchedule, DateTime currentDate, CancellationToken cancellationToken)
    {
        DateTime startDate = new(DateOnly.FromDateTime(currentDate), systemSchedule.ScheduleTime, DateTimeKind.Utc);

        await internalScheduleService.InsertScheduleAsync(
            systemSchedule.ScheduleName,
            true,
            systemSchedule.ScheduleRecurrenceType,
            startDate,
            null,
            systemSchedule.EveryX,
            systemSchedule.Weekdays,
            systemSchedule.DayOfTheMonths,
            systemSchedule.AutoRestartOnFailure,
            systemSchedule.RetryLimit,
            systemSchedule.MethodCall,
            systemSchedule.JobType,
            systemSchedule.JobLogStoreImplementationType,
            currentDate,
            cancellationToken);
    }

    #endregion Insert System Schedules

    #region Update System Schedules

    private async Task UpdateSystemSchedule(SystemSchedule systemSchedule, Schedule existingSchedule, DateTime currentDate, CancellationToken cancellationToken)
    {
        if (!existingSchedule.IsSystem)
        {
            throw new DuplicateNameException($"Scheduler - Another schedule exists with the name '{systemSchedule.ScheduleName}' and is not a system schedule");
        }

        if (!systemSchedule.ScheduleName.Equals(existingSchedule.ScheduleName, StringComparison.Ordinal))
        {
            await internalScheduleService.UpdateScheduleNameAsync(existingSchedule.ScheduleId, systemSchedule.ScheduleName, currentDate, cancellationToken);
        }

        if (!systemSchedule.IsActive.Equals(existingSchedule.IsActive))
        {
            await internalScheduleService.UpdateScheduleStatusAsync(existingSchedule, systemSchedule.IsActive, currentDate, cancellationToken);
        }

        if (systemSchedule.ScheduleRecurrenceType != existingSchedule.ScheduleRecurrenceConfig.ScheduleRecurrenceType ||
            !systemSchedule.EveryX.Equals(existingSchedule.ScheduleRecurrenceConfig.EveryX) ||
            !(systemSchedule.Weekdays ?? []).OrderBy(o => o).SequenceEqual((existingSchedule.ScheduleRecurrenceConfig.Weekdays ?? []).OrderBy(o => o)) ||
            !(systemSchedule.DayOfTheMonths ?? []).OrderBy(o => o).SequenceEqual((existingSchedule.ScheduleRecurrenceConfig.DaysOfTheMonth ?? []).OrderBy(o => o))
            )
        {
            await internalScheduleService.UpdateScheduleRecurrenceConfigsAsync(
                existingSchedule.ScheduleId,
                existingSchedule.NextOccurrenceDate,
                systemSchedule.ScheduleRecurrenceType,
                existingSchedule.ScheduleRecurrenceConfig.StartDate,
                null,
                systemSchedule.EveryX,
                systemSchedule.Weekdays?.ToArray(),
                systemSchedule.DayOfTheMonths?.ToArray(),
                currentDate,
                cancellationToken);
        }

        if (!systemSchedule.AutoRestartOnFailure.Equals(existingSchedule.ScheduleRetryConfig.AutoRestartOnFailure) || !systemSchedule.RetryLimit.Equals(existingSchedule.ScheduleRetryConfig.RetryLimit))
        {
            await internalScheduleService.UpdateScheduleRetryConfigsAsync(existingSchedule.ScheduleId, systemSchedule.AutoRestartOnFailure, systemSchedule.RetryLimit, currentDate, cancellationToken);
        }
    }

    #endregion Update System Schedules

    #endregion System Schedules

    #region Schedule Long Polling

    private async Task GetSchedulesToExecute(DateTime currentDate, CancellationToken cancellationToken)
    {
        #region Get schedules to run

        IReadOnlyCollection<SchedulerWrapper> schedulesToRun = [.. await internalScheduleService.GetScheduleToRunAsync(currentDate, cancellationToken)];

        if (schedulesToRun.Count == 0)
        {
            LogNoScheduleToExecute(currentDate);
            return;
        }

        #endregion Get schedules to run

        #region Update schedules status

        await internalScheduleService.UpdateSchedulesExecutionStatusAsync([.. schedulesToRun.Select(s => s.Schedule.ScheduleId)], ScheduleExecutionStatuses.Queued, currentDate, cancellationToken);

        foreach (SchedulerWrapper schedulerWrapper in schedulesToRun)
        {
            schedulerWrapper.Schedule.ScheduleExecutionStatus = ScheduleExecutionStatuses.Queued;
        }

        #endregion Update schedules status

        #region Insert batches

        List<SchedulerWrapper> schedulesWithExistingBatch = [.. schedulesToRun.Where(w => w.Schedule.ScheduleRetryConfig.CurrentRunningBatchId != null && (w.Schedule.ScheduleRetryConfig.NextRetryDate != null || w.Schedule.ScheduleRetryConfig.RetryCount > 0))];

        IEnumerable<Batch> existingBatches = [];
        if (schedulesWithExistingBatch.Count != 0)
        {
            //get batches for those schedules
            existingBatches = await batchService.GetBatchesAsync([.. schedulesWithExistingBatch.Select(s => s.Schedule.ScheduleRetryConfig.CurrentRunningBatchId!.Value)], cancellationToken);
        }

        IEnumerable<Schedule> scheduleToAddBatch = schedulesToRun.Except(schedulesWithExistingBatch).Select(s => s.Schedule);

        List<Batch> batches = [];
        if (scheduleToAddBatch.Any())
        {
            batches = [.. await batchService.InsertBatchesAsync(scheduleToAddBatch, currentDate, cancellationToken)];
        }

        batches.AddRange(existingBatches);

        Dictionary<Guid, Batch> dicBatchesByScheduleId = batches.ToDictionary(key => key.ScheduleId);

        #endregion Insert batches

        #region Insert jobs

        IEnumerable<Job> jobs = await jobService.InsertJobsAsync(batches.Select(s => s.BatchId), currentDate, cancellationToken);

        Dictionary<Guid, Job> dicJobs = jobs.ToDictionary(key => key.BatchId);

        #endregion Insert jobs

        #region Map Schedule Message

        List<ScheduleQueueItemPayload> scheduleQueueItemPayloads = [];
        foreach (SchedulerWrapper scheduleWrapper in schedulesToRun)
        {
            Batch batch = dicBatchesByScheduleId[scheduleWrapper.Schedule.ScheduleId];

            scheduleQueueItemPayloads.Add(new()
            {
                JobExecutionOrigin = JobExecutionOrigins.Scheduled,
                Job = dicJobs[batch.BatchId],
                JobExecutionMetadata = scheduleWrapper.JobExecutionMetadata,
                Schedule = scheduleWrapper.Schedule,
            });
        }

        #endregion Map Schedule Message

        scheduleQueueItemPayloads = [.. scheduleQueueItemPayloads.OrderBy(o =>
        {
            DateTime? date = o.Schedule!.ScheduleRetryConfig.NextRetryDate ?? o.Schedule!.NextOccurrenceDate;
            return date;
        })];

        await queueHandlerService.EnqueueAsync(scheduleQueueItemPayloads, cancellationToken);
    }

    #endregion Schedule Long Polling

    #endregion Private

    #region Logs

    [LoggerMessage(Level = LogLevel.Error, Message = "Scheduler - The schedule hosted service had an unexpected error at {CurrentDate:o}")]
    private partial void LogExecuteError(DateTime currentDate, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Scheduler - There were unexpected errors in the schedule hosted service while initializing the system schedules")]
    private partial void LogInitSystemSchedulesError(AggregateException aggregateException);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Scheduler - No schedule to run at {CurrentDate:o}")]
    private partial void LogNoScheduleToExecute(DateTime currentDate);

    #endregion Logs
}