using System.Text.Json.Serialization;

namespace Bacon.Scheduler.Models;

#region Recurrence

/// <summary>
/// Defines the schedule recurrence types
/// </summary>
/// 
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScheduleRecurrenceTypes
{
    /// <summary>
    /// Scheduler running hourly
    /// </summary>
    Hourly = 1,

    /// <summary>
    /// Scheduler running daily
    /// </summary>
    Daily = 2,

    /// <summary>
    /// Scheduler running weekly
    /// </summary>
    Weekly = 3,

    /// <summary>
    /// Scheduler running monthly
    /// </summary>
    Monthly = 4
}

/// <summary>
/// Defines the days of the week
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Weekdays
{
    /// <summary>
    /// Sunday
    /// </summary>
    Sunday = 0,

    /// <summary>
    /// Monday
    /// </summary>
    Monday = 1,

    /// <summary>
    /// Tuesday
    /// </summary>
    Tuesday = 2,

    /// <summary>
    /// Wednesday
    /// </summary>
    Wednesday = 3,

    /// <summary>
    /// Thursday
    /// </summary>
    Thursday = 4,

    /// <summary>
    /// Friday
    /// </summary>
    Friday = 5,

    /// <summary>
    /// Saturday
    /// </summary>
    Saturday = 6
}

#endregion Recurrence

#region Schedules

/// <summary>
/// Defines the current schedule status
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScheduleExecutionStatuses
{
    /// <summary>
    /// The schedule is ready to be executed
    /// </summary>
    Ready = 1,

    /// <summary>
    /// The schedule is Queued and awaiting to be run
    /// </summary>
    Queued = 2,

    /// <summary>
    /// The schedule is currently being processed
    /// </summary>
    Running = 3,

    /// <summary>
    /// The schedule failed and is waiting to be retried
    /// </summary>
    Pending = 4,

    /// <summary>
    /// The schedule has reached the maximum of retry configured and failed the last execution
    /// </summary>
    Failed = 5
}

/// <summary>
/// Defines the last schedule occurrence status
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScheduleOccurrenceStatuses
{
    /// <summary>
    /// The last job ran successfully
    /// </summary>
    Success = 1,

    /// <summary>
    /// The last job's execution failed
    /// </summary>
    Error = 2,

    /// <summary>
    /// The last job's execution ran into a system failure
    /// </summary>
    Critical = 3,

    /// <summary>
    /// The schedule was never run
    /// </summary>
    Never = 4
}

#endregion Schedules

#region Schedule Queues

/// <summary>
/// The type of concurrency rate limiter used
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConcurrencyRateLimitProviders
{
    /// <summary>
    /// Native rate limiter for non-distributed system
    /// </summary>
    Local,

    /// <summary>
    /// Concurrency using Redis for distributed systems
    /// </summary>
    Redis
}

/// <summary>
/// Defines the order that the queue
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScheduleQueueOrders
{
    /// <summary>
    /// Order from the oldest item to newest
    /// </summary>
    OldestFirst,

    /// <summary>
    /// Order from the newest item to oldest
    /// </summary>
    NewestFirst
}

#endregion Schedule Queues

#region Jobs

/// <summary>
/// Defines Job execution statuses
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobExecutionStatuses
{
    /// <summary>
    /// The execution completed with no error or warning
    /// </summary>
    Success = 1,

    /// <summary>
    /// Indicates that the Job is currently executing
    /// </summary>
    Running = 2,

    /// <summary>
    /// The execution stopped with error(s). Should only be used manually by the execution if a try catch is used and logs are used.
    /// </summary>
    Error = 3,

    /// <summary>
    /// The execution stopped with critical error(s)
    /// </summary>
    Critical = 4,

    /// <summary>
    /// The job is queued and waiting its turn to start running
    /// </summary>
    Queued = 5
}

/// <summary>
/// Defines the origin of the job
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobExecutionOrigins
{
    /// <summary>
    /// The job was executed manually
    /// </summary>
    Manual = 0,

    /// <summary>
    /// The job was Scheduled
    /// </summary>
    Scheduled = 1,
}

#endregion Jobs