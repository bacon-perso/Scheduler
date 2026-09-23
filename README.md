# Bacon.Scheduler

A storage-agnostic, distributed job scheduler for .NET 10. Register the library via dependency injection, implement a handful of persistence interfaces, declare recurring schedules against your own service methods, and let the library handle polling, queuing, retrying, and concurrency-limited execution.

## Solution layout

| Project | Description |
|---|---|
| `Bacon.Scheduler` | The library itself. No EF Core / SQL / Redis persistence for domain data ships in the box — you bring your own storage by implementing a few interfaces. |
| `Bacon.Scheduler.Samples` | An ASP.NET Core host demonstrating usage, with in-memory store implementations. |
| `Bacon.Scheduler.Tests` | NUnit test suite covering the library's internal services. |

```
dotnet build Bacon.Scheduler.slnx                     # build everything
dotnet build Bacon.Scheduler/Bacon.Scheduler.csproj    # build just the library
dotnet test Bacon.Scheduler.Tests/Bacon.Scheduler.Tests.csproj   # run the test suite
```

## How it works

- **Two background loops.** A `SchedulerHostedService` periodically polls your `IScheduleStore` for schedules that are due and enqueues them; a `QueueHandlerHostedService` dequeues and executes them, respecting a configurable concurrency limit (local or Redis-backed distributed).
- **Reflection-based execution.** Jobs are plain methods (instance or interface) that return `JobExecutionResult` (or `Task<JobExecutionResult>`). You register them via `Expression<Func<...>>`, and the library resolves the target type from DI and invokes the method for you.
- **System schedules vs. dynamic schedules.** *System schedules* are recurring jobs declared in code at startup (`AddOrUpdateSystemSchedule`) and reconciled against persisted state on every boot. *Dynamic schedules* and one-off delayed executions are created at runtime via `IScheduleService.InsertScheduleAsync` / `InsertDelayedScheduleAsync`.
- **Retries.** Failed scheduled executions retry with a linear backoff, up to a configurable limit, with an optional auto-restart after exhausting retries.
- **Bring your own storage.** You implement `IScheduleStore`, `IScheduleQueueStore`, `IBatchStore`, `IJobStore` once for the whole scheduler, plus one `IJobLogStore` per job type.

## Getting started

### 1. Implement the store interfaces

Implement `IScheduleStore`, `IScheduleQueueStore`, `IBatchStore`, and `IJobStore` against whatever backing store you use (SQL, a document store, in-memory, etc.). These are registered transient but effectively act as singletons (see `AddScheduler` below), so implementations must be stateless/thread-safe — wrap a connection pool or an `IDbContextFactory` rather than holding a live per-request `DbContext`.

`Bacon.Scheduler.Samples/Services/Stores` has runnable in-memory implementations you can use as a reference.

### 2. Implement a job and its job log store

A job is any method — on a concrete class or an interface implemented by a DI-registered service — that returns `JobExecutionResult`:

```csharp
// Bacon.Scheduler.Samples/Interfaces/Services/IMaintenanceService.cs
public interface IMaintenanceService
{
    Task<JobExecutionResult> DeleteOldUserTempEmailsAsync(int retentionInSeconds, byte someId);
}

// Bacon.Scheduler.Samples/Services/MaintenanceService.cs
public class MaintenanceService : IMaintenanceService
{
    public Task<JobExecutionResult> DeleteOldUserTempEmailsAsync(int retentionInSeconds, byte someId)
    {
        // ... do the work ...

        JobExecutionResult jobExecutionResult = new()
        {
            JobExecutionStatus = JobExecutionStatuses.Success,
            Result = jobLog,
            Exceptions = null
        };

        return Task.FromResult(jobExecutionResult);
    }
}
```

Each job type needs a matching `IJobLogStore` implementation, used to persist the result of every execution:

```csharp
// Bacon.Scheduler.Samples/Services/Stores/SystemJobStore.cs
public class SystemJobStore : IJobLogStore
{
    public ValueTask InsertJobLogAsync(Job job, JobExecutionResult jobExecutionResult, CancellationToken cancellationToken)
    {
        // ... persist job/jobExecutionResult ...
        return ValueTask.CompletedTask;
    }
}
```

### 3. Register the scheduler

```csharp
// Bacon.Scheduler.Samples/ProjectConfigs/Program.cs
builder.Services.AddTransient<IMaintenanceService, MaintenanceService>();

builder.Services
    .AddScheduler<InMemoryScheduleStore, InMemoryScheduleQueueStore, InMemoryBatchStore, InMemoryJobStore>(o =>
    {
        o.TenantId = "schedulertenant";
        o.PeriodicTimerInterval = TimeSpan.FromSeconds(10);
        o.ConcurrencyRateLimiter.ConcurrencyRateLimitProvider = ConcurrencyRateLimitProviders.Redis;
        o.ConcurrencyRateLimiter.ConnectionMultiplexerFactory = sp => connectionMultiplexer;
        o.AllowSystemScheduleManualExecution = true;
    })
    .AddOrUpdateSystemSchedule<IMaintenanceService, SystemJobStore>(
        "cleanup-temp-emails",
        isActive: true,
        ScheduleRecurrenceTypes.Weekly,
        scheduleTime: new TimeOnly(0, 30, 0),
        everyX: 1,
        weekdays: [Weekdays.Saturday],
        dayOfTheMonths: null,
        autoRestartOnFailure: true,
        retryLimit: 3,
        expression: i => i.DeleteOldUserTempEmailsAsync(20, 5))
    .Build();
```

`AddScheduler<TScheduleStore, TScheduleQueueStore, TBatchStore, TJobStore>` wires up your four store implementations, `AddOrUpdateSystemSchedule<TJob, TJobLogStore>` registers a recurring schedule against a job method (call it as many times as you have schedules), and `Build()` registers the internal services and the two background hosted services.

Setting `ConcurrencyRateLimitProvider` to `Redis` requires a reachable Redis instance and a `ConnectionMultiplexerFactory` — without one, the concurrency limiter will fail to acquire a lease. The default `Local` provider is process-local and needs no external dependency.

### 4. Create dynamic or one-off schedules at runtime

Outside of system schedules, use `IScheduleService` (injected wherever you need it) to create schedules programmatically:

```csharp
public class CleanUpService(IScheduleService scheduleService) : ICleanUpService
{
    public async Task<JobExecutionResult> CleanUpAsync(int retentionInSeconds)
    {
        // ... do the work ...

        // schedule a follow-up job to run once, 10 seconds from now
        await scheduleService.InsertDelayedScheduleAsync<CleanUpLogStore>(
            TimeSpan.FromSeconds(10),
            () => WriteLog(),
            cancellationToken);

        return jobExecutionResult;
    }
}
```

## Running the sample

`Bacon.Scheduler.Samples` expects a reachable Redis instance (`CacheSettings:RedisSettings:*` configuration) for the distributed concurrency limiter. Without one, Redis lease acquisition will fail, but the host itself still starts and serves its sample endpoints.

```
dotnet run --project Bacon.Scheduler.Samples/Bacon.Scheduler.Samples.csproj
```
