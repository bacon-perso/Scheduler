using Bacon.Scheduler.Interfaces;
using Bacon.Scheduler.Interfaces.Services.Batches;
using Bacon.Scheduler.Interfaces.Services.Jobs;
using Bacon.Scheduler.Interfaces.Services.Jobs.JobLogs;
using Bacon.Scheduler.Interfaces.Services.Queues;
using Bacon.Scheduler.Interfaces.Services.RateLimiters;
using Bacon.Scheduler.Interfaces.Services.Schedules;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Models.Schedules;
using Bacon.Scheduler.Services;
using Bacon.Scheduler.Services.Batches;
using Bacon.Scheduler.Services.Jobs;
using Bacon.Scheduler.Services.Queues;
using Bacon.Scheduler.Services.RateLimiters;
using Bacon.Scheduler.Services.Schedules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Linq.Expressions;

namespace Bacon.Scheduler.Extensions;

/// <summary>
/// Schedule extension class
/// </summary>
public static class SchedulerExtensions
{
    #region Publics

    /// <summary>
    /// Add the schedule middleware
    /// </summary>
    /// <typeparam name="TScheduleStore">The schedule store implementation</typeparam>
    /// <typeparam name="TScheduleQueueStore">The schedule queue store implementation</typeparam>
    /// <typeparam name="TBatchStore">The batch store implementation</typeparam>
    /// <typeparam name="TJobStore">The job store implementation</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="options">The scheduler configurable options</param>
    /// <returns></returns>
    public static ISchedulerBuilder AddScheduler<TScheduleStore, TScheduleQueueStore, TBatchStore, TJobStore>(this IServiceCollection services, Action<SchedulerOptions> options)
        where TScheduleStore : class, IScheduleStore
        where TScheduleQueueStore : class, IScheduleQueueStore
        where TBatchStore : class, IBatchStore
        where TJobStore : class, IJobStore
    {
        SchedulerOptions schedulerOptions = new();
        options(schedulerOptions);

        ValidateOptions(schedulerOptions);

        services.AddStore<IScheduleStore, TScheduleStore>();
        services.AddStore<IScheduleQueueStore, TScheduleQueueStore>();
        services.AddStore<IBatchStore, TBatchStore>();
        services.AddStore<IJobStore, TJobStore>();

        services.AddConcurrencyRateLimiting(schedulerOptions);

        SchedulerBuilder schedulerBuilder = new(services, []);

        schedulerBuilder.Services.Configure(options);

        return schedulerBuilder;
    }

    #region Concurrency

    private static void AddConcurrencyRateLimiting(this IServiceCollection services, SchedulerOptions schedulerOptions)
    {
        switch (schedulerOptions.ConcurrencyRateLimiter.ConcurrencyRateLimitProvider)
        {
            case ConcurrencyRateLimitProviders.Redis:
                services.AddActivatedSingleton<IConcurrencyRateLimiterService, RedisRateLimiterService>();
                break;

            default:
                services.AddActivatedSingleton<IConcurrencyRateLimiterService, LocalRateLimiterService>();
                break;
        }
    }

    #endregion Concurrency

    /// <summary>
    /// Add or update an existing system schedule at start-up. Once created the system schedules cannot be modified other than by going through the start-up again.
    /// </summary>
    /// <typeparam name="TJob">Class that will be executed when the schedule runs</typeparam>
    /// <typeparam name="TJobLogStore">The class that will be executed to log the job execution when the schedule runs</typeparam>
    /// <param name="schedulerBuilder">The scheduler builder</param>
    /// <param name="scheduleName">The unique schedule name</param>
    /// <param name="isActive">Defines if the system schedule should be active or inactive. Inactive schedules will not be executed</param>
    /// <param name="scheduleRecurrenceType">The type of recurrence</param>
    /// <param name="scheduleTime">The schedule start time in UTC.</param>
    /// <param name="everyX">Depending on the scheduleRecurrenceType, will mean every X hours/days/weeks/months (IE: every 3 days)</param>
    /// <param name="weekdays">The optional list of weekdays. Can only be used if the scheduleRecurrenceType is weekly</param>
    /// <param name="dayOfTheMonths">The optional list of days. Accepts [1-31] and 'L' for the last day of the month. Can only be used if the scheduleRecurrenceType is monthly</param>
    /// <param name="autoRestartOnFailure">If set to true, will reset the schedule status to ready after a failure (reached the retry limit). Otherwise, the schedule will stay in a failed state until manually started</param>
    /// <param name="retryLimit">Configure the amount of retries allowed. 0 for no retry, 10 for max retries. Retries are following a linear mechanism of 1min * retry count</param>
    /// <param name="expression">The function to call, including the parameters</param>
    /// <returns>The scheduler builder</returns>
    public static ISchedulerBuilder AddOrUpdateSystemSchedule<TJob, TJobLogStore>(this ISchedulerBuilder schedulerBuilder, string scheduleName, bool isActive, ScheduleRecurrenceTypes scheduleRecurrenceType, TimeOnly scheduleTime, short everyX, IEnumerable<Weekdays>? weekdays
        , IEnumerable<string>? dayOfTheMonths, bool autoRestartOnFailure, byte retryLimit, [InstantHandle] Expression<Func<TJob, Task<JobExecutionResult>>> expression) where TJob: class where TJobLogStore : class, IJobLogStore
    {
        schedulerBuilder.SystemSchedules.Add(new SystemSchedule(scheduleName, isActive, scheduleRecurrenceType, scheduleTime, everyX, weekdays?.ToArray(), dayOfTheMonths?.ToArray(), autoRestartOnFailure, retryLimit, expression, typeof(TJob), typeof(TJobLogStore)));

        return schedulerBuilder;
    }

    /// <summary>
    /// Add or update an existing system schedule at start-up. Once created the system schedules cannot be modified other than by going through the start-up again.
    /// </summary>
    /// <typeparam name="TJob">Class that will be executed when the schedule runs</typeparam>
    /// <typeparam name="TJobLogStore">The class that will be executed to log the job execution when the schedule runs</typeparam>
    /// <param name="schedulerBuilder">The scheduler builder</param>
    /// <param name="scheduleName">The unique schedule name</param>
    /// <param name="isActive">Defines if the system schedule should be active or inactive. Inactive schedules will not be executed</param>
    /// <param name="scheduleRecurrenceType">The type of recurrence</param>
    /// <param name="scheduleTime">The schedule start time in UTC.</param>
    /// <param name="everyX">Depending on the scheduleRecurrenceType, will mean every X hours/days/weeks/months (IE: every 3 days)</param>
    /// <param name="weekdays">The optional list of weekdays. Can only be used if the scheduleRecurrenceType is weekly</param>
    /// <param name="dayOfTheMonths">The optional list of days. Accepts [1-31] and 'L' for the last day of the month. Can only be used if the scheduleRecurrenceType is monthly</param>
    /// <param name="autoRestartOnFailure">If set to true, will reset the schedule status to ready after a failure (reached the retry limit). Otherwise, the schedule will stay in a failed state until manually started</param>
    /// <param name="retryLimit">Configure the amount of retries allowed. 0 for no retry, 10 for max retries. Retries are following a linear mechanism of 1min * retry count</param>
    /// <param name="expression">The function to call, including the parameters</param>
    /// <returns>The scheduler builder</returns>
    public static ISchedulerBuilder AddOrUpdateSystemSchedule<TJob, TJobLogStore>(this ISchedulerBuilder schedulerBuilder, string scheduleName, bool isActive, ScheduleRecurrenceTypes scheduleRecurrenceType, TimeOnly scheduleTime, short everyX, IEnumerable<Weekdays>? weekdays
        , IEnumerable<string>? dayOfTheMonths, bool autoRestartOnFailure, byte retryLimit, [InstantHandle] Expression<Func<TJob, JobExecutionResult>> expression) where TJob : class where TJobLogStore : class, IJobLogStore
    {
        schedulerBuilder.SystemSchedules.Add(new SystemSchedule(scheduleName, isActive, scheduleRecurrenceType, scheduleTime, everyX, weekdays?.ToArray(), dayOfTheMonths?.ToArray(), autoRestartOnFailure, retryLimit, expression, typeof(TJob), typeof(TJobLogStore)));

        return schedulerBuilder;
    }

    /// <summary>
    /// Finalizes the setup of the scheduler, validates the stores, initialize the stores as transients 
    /// </summary>
    /// <param name="schedulerBuilder"></param>
    public static void Build(this ISchedulerBuilder schedulerBuilder)
    {
        schedulerBuilder.Services.TryAddSingleton(TimeProvider.System);

        schedulerBuilder.Services.AddTransient<IScheduleValidationService, ScheduleValidationService>();

        Dictionary<string, SystemSchedule> systemSchedules = new(StringComparer.OrdinalIgnoreCase);
        foreach (SystemSchedule systemSchedule in schedulerBuilder.SystemSchedules)
        {
            systemSchedules.Add(systemSchedule.ScheduleName, systemSchedule);
        }

        schedulerBuilder.Services.TryAddSingleton(systemSchedules);

        schedulerBuilder.Services.AddTransient<IInternalScheduleService, InternalScheduleService>();
        schedulerBuilder.Services.AddTransient<IScheduleService, ScheduleService>();
        schedulerBuilder.Services.AddTransient<IJobService, JobService>();
        schedulerBuilder.Services.AddTransient<IBatchService, BatchService>();

        schedulerBuilder.Services.AddTransient<ISchedulerOccurrenceService, SchedulerOccurrenceService>();

        schedulerBuilder.Services.AddSingleton<IQueueHandlerService, QueueHandlerService>();

        schedulerBuilder.Services.AddHostedService<SchedulerHostedService>();
        schedulerBuilder.Services.AddHostedService<QueueHandlerHostedService>();
    }

    #endregion Publics

    #region Privates

    #region Stores

    // Stores are ultimately consumed by singleton services (QueueHandlerService) and singleton hosted services (SchedulerHostedService, QueueHandlerHostedService) via constructor injection, so a transient registration is
    // already captured once and held for the app's lifetime (captive dependency) regardless of the lifetime declared here. Singleton makes that explicit. Store implementations must therefore be
    // stateless/thread-safe (e.g. wrap a connection pool or an IDbContextFactory) rather than hold a live per-request/per-unit-of-work resource like a DbContext.
    private static void AddStore<TInterface, TStore>(this IServiceCollection services) where TInterface : class where TStore : class, TInterface
    {
        services.AddTransient<TInterface, TStore>();
    }

    #endregion Stores

    private static void ValidateOptions(SchedulerOptions schedulerOptions)
    {
        if (schedulerOptions.PeriodicTimerInterval < TimeSpan.FromSeconds(5) || schedulerOptions.PeriodicTimerInterval > TimeSpan.FromHours(1))
        {
            throw new ArgumentOutOfRangeException(nameof(schedulerOptions), "Scheduler - The periodic timer interval can only contain a value between 5 sec and 1 hour.");
        }

        TenantValidationService.ValidateTenantId(schedulerOptions.TenantId);

        if (schedulerOptions.ConcurrencyRateLimiter is { ConcurrencyRateLimitProvider: ConcurrencyRateLimitProviders.Redis, ConnectionMultiplexerFactory: null })
        {
            throw new InvalidOperationException("Scheduler - The ConnectionMultiplexerFactory cannot be null if the concurrency is 'redis'");
        }
    }

    #endregion Privates
}