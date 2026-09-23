using Bacon.Scheduler.Interfaces.Services.Schedules;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Models.Queues;
using Bacon.Scheduler.Models.Schedules;
using Bacon.Scheduler.Services.Batches;
using Bacon.Scheduler.Services.Jobs;
using Bacon.Scheduler.Services.Schedules;
using Bacon.Scheduler.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Data;
using System.Linq.Expressions;

namespace Bacon.Scheduler.Tests.Services.Schedules;

[TestFixture]
public class ScheduleServiceTests
{
    private static readonly Expression<Func<Task<JobExecutionResult>>> _parameterlessAsyncCall = () => new TestJob().RunAsync();

    private FakeScheduleStore _scheduleStore = null!;
    private FakeQueueHandlerService _queueHandlerService = null!;
    private FakeTimeProvider _timeProvider = null!;
    private DateTime _currentDate;
    private IScheduleService _service = null!;

    [SetUp]
    public void Setup() => Setup(new SchedulerOptions { TenantId = "test-tenant" });

    private void Setup(SchedulerOptions options)
    {
        _scheduleStore = new FakeScheduleStore();
        _queueHandlerService = new FakeQueueHandlerService();
        _currentDate = new DateTime(2026, 7, 30, 8, 0, 0, DateTimeKind.Utc);
        _timeProvider = new FakeTimeProvider(_currentDate);

        InternalScheduleService internalScheduleService = new(_scheduleStore, new SchedulerOccurrenceService(), NullLogger<InternalScheduleService>.Instance);
        ScheduleValidationService scheduleValidationService = new(_scheduleStore);
        BatchService batchService = new(new FakeBatchStore());
        JobService jobService = new(new FakeJobStore());

        _service = new ScheduleService(_scheduleStore, scheduleValidationService, internalScheduleService, batchService, jobService, _queueHandlerService, Options.Create(options), _timeProvider);
    }

    private Schedule SeedSchedule(Guid scheduleId, bool isSystem = false, bool isActive = true, JobExecutionMetadata? jobExecutionMetadata = null)
    {
        Schedule schedule = new()
        {
            ScheduleId = scheduleId,
            ScheduleName = $"Schedule-{scheduleId:N}",
            IsSystem = isSystem,
            IsActive = isActive,
            ScheduleExecutionStatus = ScheduleExecutionStatuses.Ready,
            LastOccurrenceStatus = ScheduleOccurrenceStatuses.Never,
            CreationDate = _currentDate.AddDays(-1),
            NextOccurrenceDate = _currentDate.AddDays(1),
            ScheduleRecurrenceConfig = new() { ScheduleRecurrenceType = ScheduleRecurrenceTypes.Daily, StartDate = _currentDate.AddDays(-1), EveryX = 1 },
            ScheduleRetryConfig = new() { AutoRestartOnFailure = false, RetryLimit = 3 }
        };

        _scheduleStore.Schedules[scheduleId] = new SchedulerWrapper
        {
            Schedule = schedule,
            JobExecutionMetadata = jobExecutionMetadata ?? new(typeof(ITestJob), typeof(ITestJob).GetMethod(nameof(ITestJob.RunAsync))!, [], typeof(FakeJobLogStore))
        };

        return schedule;
    }

    #region Get

    [Test]
    public async Task GetScheduleAsync_Found_ReturnsSchedule()
    {
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId);

        Schedule schedule = await _service.GetScheduleAsync(scheduleId, CancellationToken.None);

        Assert.That(schedule.ScheduleId, Is.EqualTo(scheduleId));
    }

    [Test]
    public void GetScheduleAsync_NotFound_ThrowsKeyNotFoundException()
        => Assert.ThrowsAsync<KeyNotFoundException>(async () => await _service.GetScheduleAsync(Guid.NewGuid(), CancellationToken.None));

    [Test]
    public void GetSchedulesAsync_SomeMissing_ThrowsKeyNotFoundException()
    {
        Guid found = Guid.NewGuid();
        SeedSchedule(found);

        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _service.GetSchedulesAsync([found, Guid.NewGuid()], CancellationToken.None));
    }

    #endregion

    #region Insert

    [Test]
    public void InsertScheduleAsync_RetryLimitAboveTen_ThrowsArgumentException()
        => Assert.ThrowsAsync<ArgumentException>(async () => await _service.InsertScheduleAsync<FakeJobLogStore>(
            "NewSchedule", ScheduleRecurrenceTypes.Daily, null, null, 1, null, null, false, 50, _parameterlessAsyncCall, CancellationToken.None));

    [Test]
    public void InsertScheduleAsync_StartDateNotUtc_ThrowsArgumentException()
        => Assert.ThrowsAsync<ArgumentException>(async () => await _service.InsertScheduleAsync<FakeJobLogStore>(
            "NewSchedule", ScheduleRecurrenceTypes.Daily, DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local), null, 1, null, null, false, 3, _parameterlessAsyncCall, CancellationToken.None));

    [Test]
    public void InsertScheduleAsync_StartDateInThePast_ThrowsArgumentOutOfRangeException()
        => Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await _service.InsertScheduleAsync<FakeJobLogStore>(
            "NewSchedule", ScheduleRecurrenceTypes.Daily, _currentDate.AddDays(-1), null, 1, null, null, false, 3, _parameterlessAsyncCall, CancellationToken.None));

    [Test]
    public void InsertScheduleAsync_RecurrenceTypeMismatchWithWeekdays_ThrowsArgumentException()
        => Assert.ThrowsAsync<ArgumentException>(async () => await _service.InsertScheduleAsync<FakeJobLogStore>(
            "NewSchedule", ScheduleRecurrenceTypes.Daily, null, null, 1, [Weekdays.Monday], null, false, 3, _parameterlessAsyncCall, CancellationToken.None));

    [Test]
    public void InsertScheduleAsync_MonthlyDayOutOfRange_ThrowsArgumentException()
        => Assert.ThrowsAsync<ArgumentException>(async () => await _service.InsertScheduleAsync<FakeJobLogStore>(
            "NewSchedule", ScheduleRecurrenceTypes.Monthly, null, null, 1, null, ["99"], false, 3, _parameterlessAsyncCall, CancellationToken.None));

    [Test]
    public void InsertScheduleAsync_ImpossibleAnnualFebruaryRecurrence_ThrowsArgumentException()
        // startDate deliberately in a future February (year after _currentDate's) so ValidateStartAndEndDate
        // doesn't short-circuit first with an unrelated "start date in the past" failure.
        => Assert.ThrowsAsync<ArgumentException>(async () => await _service.InsertScheduleAsync<FakeJobLogStore>(
            "NewSchedule", ScheduleRecurrenceTypes.Monthly, new DateTime(2027, 2, 1, 0, 0, 0, DateTimeKind.Utc), null, 12, null, ["30"], false, 3, _parameterlessAsyncCall, CancellationToken.None));

    [Test]
    public void InsertScheduleAsync_DuplicateName_ThrowsDuplicateNameException()
    {
        Schedule existing = SeedSchedule(Guid.NewGuid());
        existing.ScheduleName = "Existing";

        Assert.ThrowsAsync<DuplicateNameException>(async () => await _service.InsertScheduleAsync<FakeJobLogStore>(
            "Existing", ScheduleRecurrenceTypes.Daily, null, null, 1, null, null, false, 3, _parameterlessAsyncCall, CancellationToken.None));
    }

    [Test]
    public async Task InsertScheduleAsync_HappyPath_InsertsDynamicSchedule()
    {
        Schedule schedule = await _service.InsertScheduleAsync<FakeJobLogStore>(
            "NewSchedule", ScheduleRecurrenceTypes.Daily, null, null, 1, null, null, true, 4, _parameterlessAsyncCall, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(schedule.ScheduleName, Is.EqualTo("NewSchedule"));
            Assert.That(schedule.IsSystem, Is.False);
            Assert.That(schedule.ScheduleRetryConfig.AutoRestartOnFailure, Is.True);
            Assert.That(schedule.ScheduleRetryConfig.RetryLimit, Is.EqualTo((byte)4));
            Assert.That(_scheduleStore.Schedules.ContainsKey(schedule.ScheduleId), Is.True);
        });
    }

    [Test]
    public async Task InsertScheduleAsync_WithExplicitTItem_UsesInterfaceAsImplementationType()
    {
        Expression<Func<ITestJob, Task<JobExecutionResult>>> expr = i => i.RunAsync();

        Schedule schedule = await _service.InsertScheduleAsync<FakeJobLogStore, ITestJob>(
            "NewSchedule", ScheduleRecurrenceTypes.Daily, null, null, 1, null, null, false, 3, expr, CancellationToken.None);

        Assert.That(_scheduleStore.Schedules[schedule.ScheduleId].JobExecutionMetadata.JobExecutionImplementationType, Is.EqualTo(typeof(ITestJob)));
    }

    [Test]
    public void InsertDelayedScheduleAsync_DelayNotPositive_ThrowsArgumentException()
        => Assert.ThrowsAsync<ArgumentException>(async () => await _service.InsertDelayedScheduleAsync<FakeJobLogStore>(TimeSpan.Zero, _parameterlessAsyncCall, CancellationToken.None));

    [Test]
    public void InsertDelayedScheduleAsync_DelayAboveOneHour_ThrowsArgumentException()
        => Assert.ThrowsAsync<ArgumentException>(async () => await _service.InsertDelayedScheduleAsync<FakeJobLogStore>(TimeSpan.FromHours(2), _parameterlessAsyncCall, CancellationToken.None));

    [Test]
    public async Task InsertDelayedScheduleAsync_HappyPath_InsertsOneShotDailySchedule()
    {
        Schedule schedule = await _service.InsertDelayedScheduleAsync<FakeJobLogStore>(TimeSpan.FromMinutes(10), _parameterlessAsyncCall, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(schedule.ScheduleName, Does.StartWith("DelayedSchedule-"));
            Assert.That(schedule.IsSystem, Is.False);
            Assert.That(schedule.ScheduleRecurrenceConfig.ScheduleRecurrenceType, Is.EqualTo(ScheduleRecurrenceTypes.Daily));
            Assert.That(schedule.ScheduleRecurrenceConfig.StartDate, Is.EqualTo(_currentDate.AddMinutes(10)));
            Assert.That(schedule.ScheduleRecurrenceConfig.EndDate, Is.EqualTo(_currentDate.AddMinutes(10).AddHours(1)));
            Assert.That(schedule.ScheduleRetryConfig.RetryLimit, Is.EqualTo((byte)0));
        });
    }

    #endregion

    #region Run

    [Test]
    public void RunScheduleAsync_InactiveSchedule_ThrowsInvalidOperation()
    {
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId, isActive: false);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.RunScheduleAsync(scheduleId, CancellationToken.None));
    }

    [Test]
    public void RunScheduleAsync_SystemScheduleWithManualExecutionDisallowed_ThrowsInvalidOperation()
    {
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId, isSystem: true);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.RunScheduleAsync(scheduleId, CancellationToken.None));
    }

    [Test]
    public async Task RunScheduleAsync_SystemScheduleWithManualExecutionAllowed_Succeeds()
    {
        Setup(new SchedulerOptions { TenantId = "test-tenant", AllowSystemScheduleManualExecution = true });
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId, isSystem: true);

        Job job = await _service.RunScheduleAsync(scheduleId, CancellationToken.None);

        Assert.That(job, Is.Not.Null);
    }

    [Test]
    public async Task RunScheduleAsync_HappyPath_EnqueuesManualExecution()
    {
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId);

        Job job = await _service.RunScheduleAsync(scheduleId, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(_queueHandlerService.EnqueuedSingle, Has.Count.EqualTo(1));
            Assert.That(_queueHandlerService.EnqueuedSingle[0].JobExecutionOrigin, Is.EqualTo(JobExecutionOrigins.Manual));
            Assert.That(_queueHandlerService.EnqueuedSingle[0].Job.JobId, Is.EqualTo(job.JobId));
        });
    }

    #endregion

    #region Update

    [Test]
    public void UpdateScheduleNameAsync_SystemSchedule_ThrowsInvalidOperation()
    {
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId, isSystem: true);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.UpdateScheduleNameAsync(scheduleId, "NewName", CancellationToken.None));
    }

    [Test]
    public async Task UpdateScheduleNameAsync_HappyPath_UpdatesName()
    {
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId);

        Schedule updated = await _service.UpdateScheduleNameAsync(scheduleId, "Renamed", CancellationToken.None);

        Assert.That(updated.ScheduleName, Is.EqualTo("Renamed"));
    }

    [Test]
    public void UpdateScheduleStatusAsync_SystemSchedule_ThrowsInvalidOperation()
    {
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId, isSystem: true);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.UpdateScheduleStatusAsync(scheduleId, false, CancellationToken.None));
    }

    [Test]
    public async Task UpdateScheduleStatusAsync_HappyPath_UpdatesStatus()
    {
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId, isActive: true);

        Schedule updated = await _service.UpdateScheduleStatusAsync(scheduleId, false, CancellationToken.None);

        Assert.That(updated.IsActive, Is.False);
    }

    [Test]
    public void UpdateScheduleRecurrenceConfigsAsync_SystemSchedule_ThrowsInvalidOperation()
    {
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId, isSystem: true);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.UpdateScheduleRecurrenceConfigsAsync(
            scheduleId, ScheduleRecurrenceTypes.Daily, null, null, 1, null, null, CancellationToken.None));
    }

    [Test]
    public async Task UpdateScheduleRecurrenceConfigsAsync_HappyPath_RecalculatesNextOccurrenceDate()
    {
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId);

        DateTime newStart = _currentDate.AddDays(7);
        Schedule updated = await _service.UpdateScheduleRecurrenceConfigsAsync(scheduleId, ScheduleRecurrenceTypes.Daily, newStart, null, 1, null, null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(updated.ScheduleRecurrenceConfig.StartDate, Is.EqualTo(newStart));
            Assert.That(updated.NextOccurrenceDate, Is.EqualTo(newStart));
        });
    }

    [Test]
    public void UpdateScheduleRetryConfigsAsync_SystemSchedule_ThrowsInvalidOperation()
    {
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId, isSystem: true);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.UpdateScheduleRetryConfigsAsync(scheduleId, true, 5, CancellationToken.None));
    }

    [Test]
    public void UpdateScheduleRetryConfigsAsync_InvalidRetryLimit_ThrowsArgumentException()
    {
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId);

        Assert.ThrowsAsync<ArgumentException>(async () => await _service.UpdateScheduleRetryConfigsAsync(scheduleId, true, 50, CancellationToken.None));
    }

    [Test]
    public async Task UpdateScheduleRetryConfigsAsync_HappyPath_UpdatesRetryConfig()
    {
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId);

        Schedule updated = await _service.UpdateScheduleRetryConfigsAsync(scheduleId, true, 5, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(updated.ScheduleRetryConfig.AutoRestartOnFailure, Is.True);
            Assert.That(updated.ScheduleRetryConfig.RetryLimit, Is.EqualTo((byte)5));
        });
    }

    #endregion

    #region Delete

    [Test]
    public void DeleteSchedulesAsync_ContainsSystemSchedule_ThrowsInvalidOperation()
    {
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId, isSystem: true);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.DeleteSchedulesAsync([scheduleId], CancellationToken.None));
    }

    [Test]
    public async Task DeleteSchedulesAsync_HappyPath_RemovesSchedules()
    {
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId);

        await _service.DeleteSchedulesAsync([scheduleId], CancellationToken.None);

        Assert.That(_scheduleStore.Schedules.ContainsKey(scheduleId), Is.False);
    }

    #endregion
}
