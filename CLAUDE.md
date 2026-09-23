# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Bacon.Scheduler is a .NET 10 class library implementing a storage-agnostic, distributed job scheduler. Consumers register the library via DI, implement four "store" interfaces (persistence) plus one "job log store" interface per job type, register recurring "system schedules" as lambda expressions pointing at their own service methods, and the library handles polling, queuing, retrying, and concurrency-limited execution.

`Bacon.Scheduler.Samples` is an ASP.NET Core sample host demonstrating usage. It builds and runs: `ProjectConfigs/Program.cs` wires up `InMemoryScheduleStore`/`InMemoryScheduleQueueStore`/`InMemoryBatchStore`/`InMemoryJobStore` (in `Services/Stores/`) as the four required store implementations, and `Serilog.AspNetCore`/`Serilog.Sinks.Console` are referenced in the `.csproj`. It expects a reachable Redis instance (`CacheSettings:RedisSettings:*` config) for the distributed concurrency limiter — without one the scheduler's Redis lease acquisition will fail, but the host itself still starts and serves the `WeatherForecastController` endpoints.

`Bacon.Scheduler.Tests` is an NUnit test project covering the internal services (`Services/`), extensions, and hosted-service system-schedule reconciliation, using hand-written fakes (`Fakes/`) for the store interfaces rather than a mocking library.

## Commands

```
dotnet build Bacon.Scheduler.slnx                     # build everything
dotnet build Bacon.Scheduler/Bacon.Scheduler.csproj    # build just the library
dotnet test Bacon.Scheduler.Tests/Bacon.Scheduler.Tests.csproj   # run the test suite
```

## Architecture

### Entry point: `AddScheduler<...>().AddOrUpdateSystemSchedule<...>()....Build()`

`SchedulerExtensions` (`Bacon.Scheduler/Extensions/SchedulerExtensions.cs`) is the only public DI entry point:

1. `services.AddScheduler<TScheduleStore, TScheduleQueueStore, TBatchStore, TJobStore>(options)` — registers the four store implementations the consumer must supply (`IScheduleStore`, `IScheduleQueueStore`, `IBatchStore`, `IJobStore`), validates `SchedulerOptions`, and returns an `ISchedulerBuilder`.
2. Zero or more `.AddOrUpdateSystemSchedule<TJobLogStore>(...)` calls — each registers a **system schedule**: a recurring job defined in code (name, recurrence, retry policy) plus a `LambdaExpression` pointing at the method to invoke (e.g. `i => i.DoWorkAsync(args)`), captured via `Expression<Action<Task<JobExecutionResult>>>`. System schedules are reconciled against persisted schedules on startup (insert new, update changed, delete orphaned) in `SchedulerHostedService.InitSystemSchedules()`.
3. `.Build()` — registers all internal services as transient/singleton, and the two `BackgroundService`s (`SchedulerHostedService`, `QueueHandlerHostedService`) that drive the scheduler loop.

Non-system ("dynamic") schedules and one-off delayed executions are created at runtime via `IScheduleService.InsertScheduleAsync<TJobLogStore>(...)` / `InsertDelayedScheduleAsync<TJobLogStore>(...)`, which accept the same kind of `Expression<Func<...>>` pointing at instance or interface methods.

Whichever method is targeted by the expression must return `JobExecutionResult` (or `Task<JobExecutionResult>`) — that's the contract the queue handler expects when reflectively invoking the job (see below).

### Two background loops

- **`SchedulerHostedService`** (`Services/Schedules/SchedulerHostedService.cs`) — on a `PeriodicTimerInterval` (5s–1h, default 1min), long-polls `IScheduleStore.GetSchedulesToRunAsync` for schedules that are due (ready + past `NextOccurrenceDate`, or pending + past `NextRetryDate`), flips their status to `queued`, creates `Batch`/`Job` rows, and enqueues a `ScheduleQueuePayload` per due schedule via `IQueueHandlerService.EnqueueAsync`.
- **`QueueHandlerHostedService`** (`Services/Queues/QueueHandlerHostedService.cs`) — every 10s calls `IQueueHandlerService.RunQueueAsync`, which acquires a concurrency lease, peeks one item off the persisted queue (`IScheduleQueueStore`), executes it via reflection (`QueueHandlerService.ExecuteJobAsync`), writes the result to the consumer's `IJobLogStore` implementation, updates schedule/job/batch status, computes retry/next-occurrence via `SchedulerOccurrenceService`, and dequeues.

Both loops swallow and log exceptions per tick so a single bad iteration doesn't kill the hosted service.

### Job execution (`QueueHandlerService.ExecuteJobAsync`)

Reflection-based: resolves `JobExecutionMetadata.JobExecutionImplementationType` from a per-execution `AsyncServiceScope` — via `scope.ServiceProvider.GetService(...)` if it's an interface (must be registered by the consumer), else `ActivatorUtilities.CreateInstance(scope.ServiceProvider, ...)` for concrete types, so constructor-injected dependencies are supported either way. The consumer's `IJobLogStore` implementation is resolved from that same scope, so scoped dependencies in either the job or the job log store are consistently scoped to, and disposed with, that single job execution. It then invokes `JobExecutionMetadata.Method` with captured `Arguments`, and expects a `JobExecutionResult` (sync or via `Task<JobExecutionResult>`) back. The execution context is explicitly captured/replayed (`ExecutionContext.Run`) so `AsyncLocal` behaves consistently for sync and async job methods. Job-level exceptions set the job to `critical`; a `JobExecutionResult.Exceptions` payload is thrown as an `AggregateException` after being logged via the consumer's `IJobLogStore`.

Retry policy: on error/critical status for a *scheduled* (not manual) execution, `SetRetryAsync` increments `ScheduleRetryConfig.RetryCount`. Under the limit → status `pending` with a linear backoff (`currentDate.AddMinutes(retryCount)`) and the batch ID is pinned via `CurrentRunningBatchId` so the retry reuses the same batch. Over the limit → status `failed` unless `AutoRestartOnFailure` is set, in which case it goes back to `ready`.

### Recurrence calculation (`SchedulerOccurrenceService`)

Pure calculation service (no I/O) computing the next occurrence date for `hourly`/`daily`/`weekly`/`monthly` recurrence types, given `EveryX`, optional `Weekdays` (weekly only), and optional `DayOfTheMonths` (monthly only, accepts `1`-`31` and `"L"` for last day of month). Validation of these combinations (valid ranges per recurrence type, impossible Feb 30/31 configs, etc.) lives separately in `ScheduleValidationService`.

### Persistence is entirely delegated to the consumer

The library defines storage as interfaces only — it ships no EF Core / SQL / Redis persistence for domain data (Redis is used only for the optional distributed concurrency limiter, see below). Consumers must implement:

- `IScheduleStore`, `IScheduleQueueStore`, `IBatchStore`, `IJobStore` — passed as generic type args to `AddScheduler<...>` and registered **transient** (`SchedulerExtensions.AddStore`). Only `IScheduleQueueStore` is consumed directly by a singleton (`QueueHandlerService`) via constructor injection, so it's effectively captive regardless of its declared lifetime. `IScheduleStore`, `IBatchStore`, and `IJobStore` are instead consumed by their respective transient services (`ScheduleService`/`ScheduleValidationService`/`InternalScheduleService`, `BatchService`, `JobService`), so their transient registration is real. Store implementations must still be stateless/thread-safe — wrap a connection pool or an `IDbContextFactory`, don't hold a live per-request `DbContext` or other unit-of-work-scoped resource.
- `IJobLogStore` — one implementation per job/schedule, passed as the generic type arg to `AddOrUpdateSystemSchedule<TJobLogStore>` / `InsertScheduleAsync<TJobLogStore>`, resolved via `ActivatorUtilities.CreateInstance` per job execution (so it can take constructor-injected dependencies, and unlike the four stores above, can safely be scoped-resource-aware).

Domain models (`Models/Schedules/Schedule.cs`, `Models/Jobs/Job.cs`, `Models/Batches/Batch.cs`, `Models/Queues/ScheduleQueue.cs`) carry `[JsonPropertyName]` snake_case attributes — stores are expected to (de)serialize these to/from whatever backing store they use. All enums (`Models/SchedulerEnums.cs`) use `[JsonConverter(typeof(JsonStringEnumConverter))]` for string-based serialization.

### Concurrency rate limiting (`Services/RateLimiters`)

`IConcurrencyRateLimiterService` gates how many jobs run at once, selected via `SchedulerOptions.ConcurrencyRateLimiter.ConcurrencyRateLimitProvider`:

- `local` (default) — `LocalRateLimiterService`, backed by `System.Threading.RateLimiting.ConcurrencyLimiter`, process-local only.
- `redis` — `RedisRateLimiterService`, backed by `DistributedLock.Redis` (`Medallion.Threading.Redis`) for a true cross-instance distributed semaphore. Requires `ConcurrencyRateLimiterOptions.ConnectionMultiplexerFactory` to be set (validated at `AddScheduler` time).

`PermitLimit` (currently fixed at 1), `AcquireTimeout`, `LocalQueueLimit`, `RedisExpiry`, `RedisExtensionCadence` are `internal` — not yet exposed for consumer configuration.

### Expression-to-metadata resolution

`InternalScheduleService.GetJobExecutionMetadata` turns the `LambdaExpression` passed to `AddOrUpdateSystemSchedule`/`InsertScheduleAsync` into a `JobExecutionMetadata` (type + `MethodInfo` + evaluated argument values) by inspecting the `MethodCallExpression` body. For interface-typed schedules (`InsertScheduleAsync<TJobLogStore, TItem>` where `TItem` is an interface), `TypeExtensions.GetNonOpenMatchingMethod` resolves the concrete method by signature since the DI-resolved instance's concrete type differs from the interface used at registration time.

### Namespacing conventions

Folders under `Models/` and `Services/` mirror each other by domain area (`Schedules`, `Jobs`, `Batches`, `Queues`, `RateLimiters`), and interfaces mirror the same structure under `Interfaces/Services/`. Public API surface (models, options, the store/service interfaces consumers implement or call, `ISchedulerBuilder`, `SchedulerExtensions`) is `public`; internal orchestration (`SchedulerBuilder`, all `*Service` implementations, hosted services, `TenantValidationService`, extension helpers) is `internal`.
