using Bacon.Scheduler.Interfaces.RateLimiters;
using Bacon.Scheduler.Interfaces.Services.Batches;
using Bacon.Scheduler.Interfaces.Services.Jobs;
using Bacon.Scheduler.Interfaces.Services.Jobs.JobLogs;
using Bacon.Scheduler.Interfaces.Services.Queues;
using Bacon.Scheduler.Interfaces.Services.RateLimiters;
using Bacon.Scheduler.Interfaces.Services.Schedules;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Models.Queues;
using Bacon.Scheduler.Models.Schedules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Frozen;

namespace Bacon.Scheduler.Services.Queues;

internal sealed partial class QueueHandlerService(IServiceProvider serviceProvider, IInternalScheduleService internalScheduleService, IScheduleQueueStore scheduleQueueStore, IBatchService batchService, IJobService jobService, IConcurrencyRateLimiterService concurrencyRateLimiterGate
        , TimeProvider timeProvider, ILogger<QueueHandlerService> logger) : IQueueHandlerService
{
    #region Properties

    private static readonly FrozenSet<JobExecutionStatuses> _errorStatus = [JobExecutionStatuses.Critical, JobExecutionStatuses.Error];

    #endregion Properties

    #region Enqueue

    public async Task EnqueueAsync(ScheduleQueueItemPayload scheduleQueueItemPayload, CancellationToken cancellationToken)
    {
        await EnqueueAsync([scheduleQueueItemPayload], cancellationToken).ConfigureAwait(false);
    }

    public async Task EnqueueAsync(IReadOnlyCollection<ScheduleQueueItemPayload> scheduleQueueItemPayloads, CancellationToken cancellationToken)
    {
        //persist queue info
        IEnumerable<ScheduleQueueItem> scheduleQueueItems = await scheduleQueueStore.InsertSchedulePayloadsAsync(scheduleQueueItemPayloads, timeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false);

        if (!scheduleQueueItemPayloads.Count.Equals(scheduleQueueItems.Count()))
        {
            throw new KeyNotFoundException($"Scheduler - There was an error inserting schedule payloads in the queue");
        }
    }

    #endregion Enqueue

    #region Run

    public async Task RunQueueAsync(CancellationToken cancellationToken)
    {
        IConcurrencyRateLimitHandle? concurrencyRateLimitHandle = await concurrencyRateLimiterGate.TryAcquireAsync(cancellationToken);

        //lease not acquired
        if (concurrencyRateLimitHandle is null)
        {
            LogCurrentDateDebugWithGuard(LogRunQueueLeaseNotAcquired);
            
            return;
        }

        LogCurrentDateDebugWithGuard(LogRunQueueLeaseAcquired);

        ScheduleQueueItem? scheduleQueueItem = await PeekAsync(cancellationToken);

        // no item in the queue left
        if (scheduleQueueItem is null)
        {
            await concurrencyRateLimitHandle.DisposeAsync();

            LogCurrentDateDebugWithGuard(LogRunQueueEmptyQueueLeaseReleased);

            return;
        }

        LogCurrentDateDebugWithGuard(LogRunQueueExecutingJob);

        scheduleQueueItem.Payload = await ExecuteJobAsync(scheduleQueueItem.Payload, cancellationToken);

        LogCurrentDateDebugWithGuard(LogRunQueueFinishedExecutingJob);

        await DequeueAsync(scheduleQueueItem, cancellationToken);

        LogCurrentDateDebugWithGuard(LogRunQueueDeletedQueue);

        await concurrencyRateLimitHandle.DisposeAsync();

        LogCurrentDateDebugWithGuard(LogRunQueueLeaseReleased);
    }

    #endregion Run

    #region Private

    private async Task<ScheduleQueueItemPayload> ExecuteJobAsync(ScheduleQueueItemPayload scheduleQueueItemPayload, CancellationToken cancellationToken)
    {
        DateTime currentDate = timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            scheduleQueueItemPayload.Schedule = await internalScheduleService.UpdateScheduleExecutionStatusAsync(scheduleQueueItemPayload.Schedule.ScheduleId, ScheduleExecutionStatuses.Running, currentDate, cancellationToken);

            scheduleQueueItemPayload.Job = await jobService.UpdateJobExecutionStatusAsync(scheduleQueueItemPayload.Job.JobId, JobExecutionStatuses.Running, currentDate, cancellationToken);

            #region Execute Job

            await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

            object instanceActivator = scheduleQueueItemPayload.JobExecutionMetadata.JobExecutionImplementationType.IsInterface ? 
                scope.ServiceProvider.GetService(scheduleQueueItemPayload.JobExecutionMetadata.JobExecutionImplementationType) ?? throw new InvalidOperationException($"The {scheduleQueueItemPayload.JobExecutionMetadata.JobExecutionImplementationType.Name} has not been configured") 
                : ActivatorUtilities.CreateInstance(scope.ServiceProvider, scheduleQueueItemPayload.JobExecutionMetadata.JobExecutionImplementationType);

            BackgroundJobMethod backgroundJobMethod = new(scheduleQueueItemPayload.JobExecutionMetadata.Method, instanceActivator, [.. scheduleQueueItemPayload.JobExecutionMetadata.Arguments.Select(s => s.Value)]);

            ExecutionContext? executionContext = ExecutionContext.Capture();

            object? returnObject = null;
            if (executionContext == null)
            {
                returnObject = backgroundJobMethod.Invoke();
            }
            else
            {
                /*
                Asynchronous methods started with the TaskScheduler.StartNew method, capture the current execution context by default and call the ExecutionContext.Run method on a thread pool thread to pass the current AsyncLocal values there.

                Synchronous methods don't need to capture the current execution context because the thread is not changed. However, any updates to AsyncLocal values that happen inside the background job method are passed back to the calling thread,
                expanding their lifetime to the lifetime of the thread itself. This can result in memory leaks and possibly affect future background job method executions in unexpected ways.

                To avoid this and to have the same behavior of AsyncLocal between synchronous and asynchronous methods, we run synchronous ones in a captured execution context. The ExecutionContext.Run method ensures that AsyncLocal values will be modified
                only in the captured context and will not flow back to the parent context.
                */
                ExecutionContext.Run(executionContext, InvokeSynchronouslyInternal, backgroundJobMethod);

                returnObject = backgroundJobMethod.Result;
            }

            JobExecutionResult jobExecutionResult = returnObject switch
            {
                // The contract requires the invoked method to return JobExecutionResult or Task<JobExecutionResult> — awaiting here (instead of blocking on Task.Result via reflection) keeps this truly asynchronous.
                Task<JobExecutionResult> typedTask => await typedTask.ConfigureAwait(false) ?? throw new InvalidCastException("The job result is missing a value"),
                JobExecutionResult syncResult => syncResult,
                _ => throw new InvalidCastException("The job result cannot be null")
            };

            #endregion Execute Job

            #region Job Store Logs

            IJobLogStore jobLogStoreImplementation = (IJobLogStore)ActivatorUtilities.CreateInstance(scope.ServiceProvider, scheduleQueueItemPayload.JobExecutionMetadata.JobLogStoreImplementationType);

            await jobLogStoreImplementation.InsertJobLogAsync(scheduleQueueItemPayload.Job, jobExecutionResult, cancellationToken);

            #endregion Job Store Logs

            if (jobExecutionResult.Exceptions?.Any() == true)
            {
                throw new AggregateException(jobExecutionResult.Exceptions);
            }

            scheduleQueueItemPayload.Job.JobExecutionStatus = jobExecutionResult.JobExecutionStatus;

            scheduleQueueItemPayload.Schedule.ScheduleExecutionStatus = _errorStatus.Contains(jobExecutionResult.JobExecutionStatus) ? ScheduleExecutionStatuses.Failed : ScheduleExecutionStatuses.Ready;
        }
        catch (Exception e)
        {
            LogExecuteJobError(scheduleQueueItemPayload.Job.JobId, e);

            scheduleQueueItemPayload.Job.JobExecutionStatus = JobExecutionStatuses.Critical;
            scheduleQueueItemPayload.Schedule.ScheduleExecutionStatus = ScheduleExecutionStatuses.Failed;
        }

        currentDate = timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            scheduleQueueItemPayload.Job = await jobService.UpdateJobExecutionStatusAsync(scheduleQueueItemPayload.Job.JobId, scheduleQueueItemPayload.Job.JobExecutionStatus, currentDate, cancellationToken);

            if (_errorStatus.Contains(scheduleQueueItemPayload.Job.JobExecutionStatus) && scheduleQueueItemPayload.JobExecutionOrigin == JobExecutionOrigins.Scheduled)
            {
                scheduleQueueItemPayload.Schedule = await SetRetryAsync(scheduleQueueItemPayload, currentDate, cancellationToken);
            }

            scheduleQueueItemPayload.Schedule = await internalScheduleService.UpdateScheduleExecutionStatusAsync(scheduleQueueItemPayload.Schedule.ScheduleId, scheduleQueueItemPayload.Schedule.ScheduleExecutionStatus, currentDate, cancellationToken);
            scheduleQueueItemPayload.Schedule = await UpdateScheduleLastRunStatusAsync(scheduleQueueItemPayload, currentDate, cancellationToken);

            if (scheduleQueueItemPayload.Schedule.ScheduleExecutionStatus == ScheduleExecutionStatuses.Failed)
            {
                await batchService.UpdateBatchEndDateAsync(scheduleQueueItemPayload.Job.BatchId, currentDate, cancellationToken);
            }
            else if (scheduleQueueItemPayload.Schedule.ScheduleExecutionStatus == ScheduleExecutionStatuses.Ready)
            {
                //reset retry count to 0 if success
                if (scheduleQueueItemPayload.Schedule.ScheduleRetryConfig.RetryCount > 0)
                {
                    scheduleQueueItemPayload.Schedule = await internalScheduleService.UpdateScheduleRetryCountAsync(scheduleQueueItemPayload.Schedule.ScheduleId, 0, currentDate, cancellationToken);
                }

                if (scheduleQueueItemPayload.JobExecutionOrigin == JobExecutionOrigins.Scheduled)
                {
                    scheduleQueueItemPayload.Schedule = await internalScheduleService.UpdateScheduleNextOccurrenceDateAsync(scheduleQueueItemPayload.Schedule, currentDate, cancellationToken);
                }

                await batchService.UpdateBatchEndDateAsync(scheduleQueueItemPayload.Job.BatchId, currentDate, cancellationToken);
            }
            
            return scheduleQueueItemPayload;
        }
        catch (Exception e)
        {
            LogFinalizingJobExecutionError(scheduleQueueItemPayload.Job.JobId, e);
            return scheduleQueueItemPayload;
        }
    }

    private static void InvokeSynchronouslyInternal(object? state)
    {
        if (state == null)
        {
            return;
        }

        ((BackgroundJobMethod)state).Invoke();
    }

    private async Task<Schedule> SetRetryAsync(ScheduleQueueItemPayload scheduleQueueItemPayload, DateTime currentDate, CancellationToken cancellationToken)
    {
        scheduleQueueItemPayload.Schedule!.ScheduleRetryConfig.RetryCount += 1;

        if (scheduleQueueItemPayload.Schedule.ScheduleRetryConfig.RetryCount > scheduleQueueItemPayload.Schedule.ScheduleRetryConfig.RetryLimit)
        {
            //resets the current running batch ID
            scheduleQueueItemPayload.Schedule = await internalScheduleService.UpdateScheduleCurrentRunningBatchIdAsync(scheduleQueueItemPayload.Schedule.ScheduleId, null, currentDate, cancellationToken);

            scheduleQueueItemPayload.Schedule.ScheduleExecutionStatus = scheduleQueueItemPayload.Schedule.ScheduleRetryConfig.AutoRestartOnFailure ? ScheduleExecutionStatuses.Ready : ScheduleExecutionStatuses.Failed;

            return scheduleQueueItemPayload.Schedule;
        }

        LogJobRetry(scheduleQueueItemPayload.Schedule.ScheduleRetryConfig.RetryCount, scheduleQueueItemPayload.Schedule.ScheduleName, scheduleQueueItemPayload.Schedule.ScheduleId);

        scheduleQueueItemPayload.Schedule = await internalScheduleService.UpdateScheduleRetryCountAsync(scheduleQueueItemPayload.Schedule.ScheduleId, scheduleQueueItemPayload.Schedule.ScheduleRetryConfig.RetryCount, currentDate, cancellationToken);

        scheduleQueueItemPayload.Schedule = await internalScheduleService.UpdateScheduleCurrentRunningBatchIdAsync(scheduleQueueItemPayload.Schedule.ScheduleId, scheduleQueueItemPayload.Job.BatchId, currentDate, cancellationToken);

        scheduleQueueItemPayload.Schedule.ScheduleExecutionStatus = ScheduleExecutionStatuses.Pending;

        return scheduleQueueItemPayload.Schedule;
    }

    #region Schedule

    private async Task<Schedule> UpdateScheduleLastRunStatusAsync(ScheduleQueueItemPayload scheduleQueueItemPayload, DateTime currentDate, CancellationToken cancellationToken)
    {
        ScheduleOccurrenceStatuses scheduleOccurrenceStatus = scheduleQueueItemPayload.Job.JobExecutionStatus switch
        {
            JobExecutionStatuses.Success => ScheduleOccurrenceStatuses.Success,
            JobExecutionStatuses.Error => ScheduleOccurrenceStatuses.Error,
            JobExecutionStatuses.Critical => ScheduleOccurrenceStatuses.Critical,
            _ => ScheduleOccurrenceStatuses.Critical
        };

        return await internalScheduleService.UpdateScheduleLastRunStatusAndDateAsync(scheduleQueueItemPayload.Schedule!.ScheduleId, scheduleOccurrenceStatus, scheduleQueueItemPayload.Job.JobEndDate ?? currentDate, currentDate, cancellationToken);
    }

    #endregion Schedule

    #region Schedule Queue

    private async Task<ScheduleQueueItem?> PeekAsync(CancellationToken cancellationToken)
    {
        try
        {
            ScheduleQueueItem? scheduleQueueItem = await scheduleQueueStore.PeekScheduleQueuePayloadAsync(ScheduleQueueOrders.OldestFirst, cancellationToken);

            if (scheduleQueueItem is null)
            {
                LogPeekEmptyQueue();
            }

            return scheduleQueueItem;
        }
        catch (Exception e)
        {
            LogPeekQueueError(e);

            throw new InvalidOperationException("Error peeking an item in the queue handler", e);
        }
    }

    private async Task DequeueAsync(ScheduleQueueItem scheduleQueueItem, CancellationToken cancellationToken)
    {
        try
        {
            await scheduleQueueStore.DeleteScheduleQueueAsync(scheduleQueueItem.ScheduleQueueItemId, cancellationToken);
        }
        catch (Exception e)
        {
            LogDequeueError(scheduleQueueItem, e);

            throw new InvalidOperationException("Error dequeueing an item in the queue handler", e);
        }
    }

    #endregion Schedule Queue

    #endregion Private

    #region Logs

    private void LogCurrentDateDebugWithGuard(Action<DateTime> log)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            log(timeProvider.GetUtcNow().UtcDateTime);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Scheduler - Lease not acquired at {CurrentDate:o}")]
    private partial void LogRunQueueLeaseNotAcquired(DateTime currentDate);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Scheduler - Lease acquired at {CurrentDate:o}")]
    private partial void LogRunQueueLeaseAcquired(DateTime currentDate);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Scheduler - Empty schedule queue. Lease released at {CurrentDate:o}")]
    private partial void LogRunQueueEmptyQueueLeaseReleased(DateTime currentDate);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Scheduler - Executing job at {CurrentDate:o}")]
    private partial void LogRunQueueExecutingJob(DateTime currentDate);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Scheduler - Job finished executing at {CurrentDate:o}")]
    private partial void LogRunQueueFinishedExecutingJob(DateTime currentDate);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Scheduler - Schedule queue deleted at {CurrentDate:o}")]
    private partial void LogRunQueueDeletedQueue(DateTime currentDate);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Scheduler - Lease released at {CurrentDate:o}")]
    private partial void LogRunQueueLeaseReleased(DateTime currentDate);

    [LoggerMessage(Level = LogLevel.Error, Message = "Scheduler - The job {JobId} failed to execute")]
    private partial void LogExecuteJobError(Guid jobId, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Scheduler - Error when finalizing the job execution for the job '{JobId}'")]
    private partial void LogFinalizingJobExecutionError(Guid jobId, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Scheduler - Retry number {RetryCount} of schedule '{ScheduleName}' ({ScheduleId})")]
    private partial void LogJobRetry(byte retryCount, string scheduleName, Guid scheduleId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Scheduler - The queue is empty and cannot be peeked")]
    private partial void LogPeekEmptyQueue();

    [LoggerMessage(Level = LogLevel.Error, Message = "Scheduler - There was an error peeking an item from the queue.")]
    private partial void LogPeekQueueError(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Scheduler - There was an error removing an item from the queue. Schedule Message : {@ScheduleQueueItem}")]
    private partial void LogDequeueError(ScheduleQueueItem scheduleQueueItem, Exception exception);

    #endregion Logs
}