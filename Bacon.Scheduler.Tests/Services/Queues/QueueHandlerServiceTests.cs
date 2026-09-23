using Bacon.Scheduler.Interfaces.Services.Queues;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Batches;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Models.Queues;
using Bacon.Scheduler.Models.Schedules;
using Bacon.Scheduler.Services.Batches;
using Bacon.Scheduler.Services.Jobs;
using Bacon.Scheduler.Services.Queues;
using Bacon.Scheduler.Services.Schedules;
using Bacon.Scheduler.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bacon.Scheduler.Tests.Services.Queues;

[TestFixture]
public class QueueHandlerServiceTests
{
    private FakeScheduleStore _scheduleStore = null!;
    private FakeScheduleQueueStore _scheduleQueueStore = null!;
    private FakeBatchStore _batchStore = null!;
    private FakeJobStore _jobStore = null!;
    private FakeConcurrencyRateLimiterService _rateLimiter = null!;
    private FakeTimeProvider _timeProvider = null!;
    private DateTime _currentDate;
    private QueueHandlerService _service = null!;

    [SetUp]
    public void Setup()
    {
        _scheduleStore = new FakeScheduleStore();
        _scheduleQueueStore = new FakeScheduleQueueStore();
        _batchStore = new FakeBatchStore();
        _jobStore = new FakeJobStore();
        _rateLimiter = new FakeConcurrencyRateLimiterService();
        _currentDate = new DateTime(2026, 7, 30, 8, 0, 0, DateTimeKind.Utc);
        _timeProvider = new FakeTimeProvider(_currentDate);
        FakeJobLogStore.InsertedLogs.Clear();

        InternalScheduleService internalScheduleService = new(_scheduleStore, new SchedulerOccurrenceService(), NullLogger<InternalScheduleService>.Instance);
        BatchService batchService = new(_batchStore);
        JobService jobService = new(_jobStore);
        ServiceProvider emptyServiceProvider = new ServiceCollection().BuildServiceProvider();

        _service = new QueueHandlerService(emptyServiceProvider, internalScheduleService, _scheduleQueueStore, batchService, jobService, _rateLimiter, _timeProvider, NullLogger<QueueHandlerService>.Instance);
    }

    [TearDown]
    public async Task TearDown() => await _rateLimiter.DisposeAsync();

    private (Schedule schedule, Job job, Guid batchId) SeedQueueItem(Type jobType, JobExecutionOrigins origin = JobExecutionOrigins.Scheduled, byte retryLimit = 3, byte retryCount = 0, bool autoRestartOnFailure = false)
    {
        Guid scheduleId = Guid.NewGuid();
        Guid jobId = Guid.NewGuid();
        Guid batchId = Guid.NewGuid();

        Schedule schedule = new()
        {
            ScheduleId = scheduleId,
            ScheduleName = $"Schedule-{scheduleId:N}",
            IsSystem = false,
            IsActive = true,
            ScheduleExecutionStatus = ScheduleExecutionStatuses.Running,
            LastOccurrenceStatus = ScheduleOccurrenceStatuses.Never,
            CreationDate = _currentDate.AddDays(-1),
            NextOccurrenceDate = _currentDate.AddDays(1),
            ScheduleRecurrenceConfig = new() { ScheduleRecurrenceType = ScheduleRecurrenceTypes.Daily, StartDate = _currentDate.AddDays(-1), EveryX = 1 },
            ScheduleRetryConfig = new() { AutoRestartOnFailure = autoRestartOnFailure, RetryLimit = retryLimit, RetryCount = retryCount }
        };

        JobExecutionMetadata jobExecutionMetadata = new(jobType, jobType.GetMethod(nameof(SucceedingQueueJob.RunAsync))!, [], typeof(FakeJobLogStore));

        _scheduleStore.Schedules[scheduleId] = new SchedulerWrapper { Schedule = schedule, JobExecutionMetadata = jobExecutionMetadata };

        Job job = new() { JobId = jobId, BatchId = batchId, JobExecutionStatus = JobExecutionStatuses.Running, JobStartDate = _currentDate, CreationDate = _currentDate };
        _jobStore.Jobs[jobId] = job;

        _batchStore.Batches[batchId] = new Batch { BatchId = batchId, ScheduleId = scheduleId, BatchStartDate = _currentDate };

        ScheduleQueueItemPayload payload = new()
        {
            JobExecutionOrigin = origin,
            Job = job,
            JobExecutionMetadata = jobExecutionMetadata,
            Schedule = schedule
        };

        ScheduleQueueItem queueItem = new() { ScheduleQueueItemId = Guid.NewGuid(), Payload = payload, QueueCreationDate = _currentDate };
        _scheduleQueueStore.Items[queueItem.ScheduleQueueItemId] = queueItem;

        return (schedule, job, batchId);
    }

    #region Enqueue

    [Test]
    public async Task EnqueueAsync_Single_InsertsIntoQueueStore()
    {
        Schedule schedule = new()
        {
            ScheduleId = Guid.NewGuid(),
            ScheduleName = "S",
            IsSystem = false,
            IsActive = true,
            ScheduleExecutionStatus = ScheduleExecutionStatuses.Queued,
            LastOccurrenceStatus = ScheduleOccurrenceStatuses.Never,
            CreationDate = _currentDate,
            ScheduleRecurrenceConfig = new() { ScheduleRecurrenceType = ScheduleRecurrenceTypes.Daily, StartDate = _currentDate, EveryX = 1 },
            ScheduleRetryConfig = new()
        };
        Job job = new() { JobId = Guid.NewGuid(), BatchId = Guid.NewGuid(), JobExecutionStatus = JobExecutionStatuses.Queued, JobStartDate = _currentDate, CreationDate = _currentDate };
        ScheduleQueueItemPayload payload = new()
        {
            JobExecutionOrigin = JobExecutionOrigins.Manual,
            Job = job,
            JobExecutionMetadata = new(typeof(SucceedingQueueJob), typeof(SucceedingQueueJob).GetMethod(nameof(SucceedingQueueJob.RunAsync))!, [], typeof(FakeJobLogStore)),
            Schedule = schedule
        };

        await _service.EnqueueAsync(payload, CancellationToken.None);

        Assert.That(_scheduleQueueStore.Items, Has.Count.EqualTo(1));
    }

    private sealed class DroppingScheduleQueueStore : IScheduleQueueStore
    {
        public ValueTask<int> CountAsync(CancellationToken cancellationToken) => ValueTask.FromResult(0);

        public ValueTask<ScheduleQueueItem?> PeekScheduleQueuePayloadAsync(ScheduleQueueOrders scheduleQueueOrder, CancellationToken cancellationToken) => ValueTask.FromResult<ScheduleQueueItem?>(null);

        public ValueTask<IEnumerable<ScheduleQueueItem>> InsertSchedulePayloadsAsync(IEnumerable<ScheduleQueueItemPayload> scheduleQueueItemPayloads, DateTime currentDate, CancellationToken cancellationToken)
            => ValueTask.FromResult(Enumerable.Empty<ScheduleQueueItem>()); // always drops everything

        public ValueTask DeleteScheduleQueueAsync(Guid scheduleQueueId, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    [Test]
    public void EnqueueAsync_StoreDropsItems_ThrowsKeyNotFoundException()
    {
        QueueHandlerService service = new(new ServiceCollection().BuildServiceProvider(), new InternalScheduleService(_scheduleStore, new SchedulerOccurrenceService(), NullLogger<InternalScheduleService>.Instance),
            new DroppingScheduleQueueStore(), new BatchService(_batchStore), new JobService(_jobStore), _rateLimiter, _timeProvider, NullLogger<QueueHandlerService>.Instance);

        Schedule schedule = new()
        {
            ScheduleId = Guid.NewGuid(),
            ScheduleName = "S",
            IsSystem = false,
            IsActive = true,
            ScheduleExecutionStatus = ScheduleExecutionStatuses.Queued,
            LastOccurrenceStatus = ScheduleOccurrenceStatuses.Never,
            CreationDate = _currentDate,
            ScheduleRecurrenceConfig = new() { ScheduleRecurrenceType = ScheduleRecurrenceTypes.Daily, StartDate = _currentDate, EveryX = 1 },
            ScheduleRetryConfig = new()
        };
        Job job = new() { JobId = Guid.NewGuid(), BatchId = Guid.NewGuid(), JobExecutionStatus = JobExecutionStatuses.Queued, JobStartDate = _currentDate, CreationDate = _currentDate };
        ScheduleQueueItemPayload payload = new()
        {
            JobExecutionOrigin = JobExecutionOrigins.Manual,
            Job = job,
            JobExecutionMetadata = new(typeof(SucceedingQueueJob), typeof(SucceedingQueueJob).GetMethod(nameof(SucceedingQueueJob.RunAsync))!, [], typeof(FakeJobLogStore)),
            Schedule = schedule
        };

        Assert.ThrowsAsync<KeyNotFoundException>(async () => await service.EnqueueAsync(payload, CancellationToken.None));
    }

    #endregion

    #region RunQueueAsync - lease / empty queue

    [Test]
    public async Task RunQueueAsync_LeaseNotAcquired_DoesNothing()
    {
        _rateLimiter.GrantLease = false;
        SeedQueueItem(typeof(SucceedingQueueJob));

        await _service.RunQueueAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(_rateLimiter.AcquireCallCount, Is.EqualTo(1));
            Assert.That(_scheduleQueueStore.Items, Has.Count.EqualTo(1)); // untouched
        });
    }

    [Test]
    public async Task RunQueueAsync_EmptyQueue_ReleasesLease()
    {
        await _service.RunQueueAsync(CancellationToken.None);

        Assert.That(_rateLimiter.LastHandle!.IsDisposed, Is.True);
    }

    #endregion

    #region RunQueueAsync - success

    [Test]
    public async Task RunQueueAsync_SuccessfulScheduledExecution_UpdatesStatusesAndAdvancesOccurrence()
    {
        (Schedule schedule, Job job, Guid batchId) = SeedQueueItem(typeof(SucceedingQueueJob));
        DateTime originalNextOccurrence = schedule.NextOccurrenceDate!.Value;

        await _service.RunQueueAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(_scheduleStore.Schedules[schedule.ScheduleId].Schedule.ScheduleExecutionStatus, Is.EqualTo(ScheduleExecutionStatuses.Ready));
            Assert.That(_jobStore.Jobs[job.JobId].JobExecutionStatus, Is.EqualTo(JobExecutionStatuses.Success));
            Assert.That(_jobStore.Jobs[job.JobId].JobEndDate, Is.Not.Null);
            Assert.That(_scheduleStore.Schedules[schedule.ScheduleId].Schedule.NextOccurrenceDate, Is.Not.EqualTo(originalNextOccurrence));
            Assert.That(_batchStore.Batches[batchId].BatchEndDate, Is.Not.Null);
            Assert.That(_scheduleQueueStore.Items, Is.Empty);
            Assert.That(FakeJobLogStore.InsertedLogs.Select(l => l.Job.JobId), Does.Contain(job.JobId));
        });
    }

    [Test]
    public async Task RunQueueAsync_ErrorResultWithoutException_TreatedSameAsCriticalFailure()
    {
        (Schedule schedule, Job job, _) = SeedQueueItem(typeof(ErrorResultQueueJob), retryLimit: 3, retryCount: 0);

        await _service.RunQueueAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(_jobStore.Jobs[job.JobId].JobExecutionStatus, Is.EqualTo(JobExecutionStatuses.Error));
            Assert.That(_scheduleStore.Schedules[schedule.ScheduleId].Schedule.ScheduleExecutionStatus, Is.EqualTo(ScheduleExecutionStatuses.Pending));
            Assert.That(_scheduleStore.Schedules[schedule.ScheduleId].Schedule.ScheduleRetryConfig.RetryCount, Is.EqualTo((byte)1));
        });
    }

    [Test]
    public async Task RunQueueAsync_SuccessfulExecution_ResetsPriorRetryCountToZero()
    {
        (Schedule schedule, _, _) = SeedQueueItem(typeof(SucceedingQueueJob), retryLimit: 3, retryCount: 2);

        await _service.RunQueueAsync(CancellationToken.None);

        Assert.That(_scheduleStore.Schedules[schedule.ScheduleId].Schedule.ScheduleRetryConfig.RetryCount, Is.EqualTo((byte)0));
    }

    #endregion

    #region RunQueueAsync - failure / retry

    [Test]
    public async Task RunQueueAsync_ThrowingJob_SetsJobCritical()
    {
        (_, Job job, _) = SeedQueueItem(typeof(ThrowingQueueJob));

        await _service.RunQueueAsync(CancellationToken.None);

        Assert.That(_jobStore.Jobs[job.JobId].JobExecutionStatus, Is.EqualTo(JobExecutionStatuses.Critical));
    }

    [Test]
    public async Task RunQueueAsync_FailingScheduledExecutionUnderRetryLimit_SetsPendingWithLinearBackoffAndPinnedBatch()
    {
        (Schedule schedule, _, Guid batchId) = SeedQueueItem(typeof(ThrowingQueueJob), retryLimit: 3, retryCount: 0);

        await _service.RunQueueAsync(CancellationToken.None);

        Schedule updated = _scheduleStore.Schedules[schedule.ScheduleId].Schedule;

        Assert.Multiple(() =>
        {
            Assert.That(updated.ScheduleExecutionStatus, Is.EqualTo(ScheduleExecutionStatuses.Pending));
            Assert.That(updated.ScheduleRetryConfig.RetryCount, Is.EqualTo((byte)1));
            Assert.That(updated.ScheduleRetryConfig.NextRetryDate, Is.EqualTo(_currentDate.AddMinutes(1)));
            Assert.That(updated.ScheduleRetryConfig.CurrentRunningBatchId, Is.EqualTo(batchId));
        });
    }

    [Test]
    public async Task RunQueueAsync_FailingScheduledExecutionOverRetryLimitNoAutoRestart_SetsFailedAndClearsBatchId()
    {
        (Schedule schedule, _, _) = SeedQueueItem(typeof(ThrowingQueueJob), retryLimit: 0, retryCount: 0, autoRestartOnFailure: false);

        await _service.RunQueueAsync(CancellationToken.None);

        Schedule updated = _scheduleStore.Schedules[schedule.ScheduleId].Schedule;

        Assert.Multiple(() =>
        {
            Assert.That(updated.ScheduleExecutionStatus, Is.EqualTo(ScheduleExecutionStatuses.Failed));
            Assert.That(updated.ScheduleRetryConfig.CurrentRunningBatchId, Is.Null);
        });
    }

    [Test]
    public async Task RunQueueAsync_FailingScheduledExecutionOverRetryLimitWithAutoRestart_SetsReady()
    {
        (Schedule schedule, _, _) = SeedQueueItem(typeof(ThrowingQueueJob), retryLimit: 0, retryCount: 0, autoRestartOnFailure: true);

        await _service.RunQueueAsync(CancellationToken.None);

        Assert.That(_scheduleStore.Schedules[schedule.ScheduleId].Schedule.ScheduleExecutionStatus, Is.EqualTo(ScheduleExecutionStatuses.Ready));
    }

    [Test]
    public async Task RunQueueAsync_FailingManualExecution_DoesNotTriggerRetryLogic()
    {
        (Schedule schedule, _, _) = SeedQueueItem(typeof(ThrowingQueueJob), JobExecutionOrigins.Manual, retryLimit: 3, retryCount: 0);

        await _service.RunQueueAsync(CancellationToken.None);

        Schedule updated = _scheduleStore.Schedules[schedule.ScheduleId].Schedule;

        Assert.Multiple(() =>
        {
            Assert.That(updated.ScheduleExecutionStatus, Is.EqualTo(ScheduleExecutionStatuses.Failed));
            Assert.That(updated.ScheduleRetryConfig.RetryCount, Is.EqualTo((byte)0)); // never incremented - SetRetryAsync only runs for Scheduled origin
        });
    }

    #endregion
}
