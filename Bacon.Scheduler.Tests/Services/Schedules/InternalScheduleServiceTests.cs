using Bacon.Scheduler.Interfaces.Services.Schedules;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Models.Schedules;
using Bacon.Scheduler.Services.Schedules;
using Bacon.Scheduler.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq.Expressions;

namespace Bacon.Scheduler.Tests.Services.Schedules;

[TestFixture]
public class InternalScheduleServiceTests
{
    private FakeScheduleStore _scheduleStore = null!;
    private IInternalScheduleService _service = null!;
    private DateTime _currentDate;

    [SetUp]
    public void Setup()
    {
        _scheduleStore = new FakeScheduleStore();
        _service = new InternalScheduleService(_scheduleStore, new SchedulerOccurrenceService(), NullLogger<InternalScheduleService>.Instance);
        _currentDate = new DateTime(2026, 7, 30, 8, 0, 0, DateTimeKind.Utc);
    }

    private Schedule SeedSchedule(Guid scheduleId, ScheduleRecurrenceTypes recurrenceType = ScheduleRecurrenceTypes.Daily, short everyX = 1, DateTime? startDate = null, DateTime? nextOccurrenceDate = null, bool isActive = true)
    {
        Schedule schedule = new()
        {
            ScheduleId = scheduleId,
            ScheduleName = $"Schedule-{scheduleId:N}",
            IsSystem = false,
            IsActive = isActive,
            ScheduleExecutionStatus = ScheduleExecutionStatuses.Ready,
            LastOccurrenceStatus = ScheduleOccurrenceStatuses.Never,
            CreationDate = _currentDate.AddDays(-1),
            NextOccurrenceDate = nextOccurrenceDate,
            ScheduleRecurrenceConfig = new()
            {
                ScheduleRecurrenceType = recurrenceType,
                StartDate = startDate ?? _currentDate.AddDays(-1),
                EveryX = everyX
            },
            ScheduleRetryConfig = new() { AutoRestartOnFailure = false, RetryLimit = 3 }
        };

        _scheduleStore.Schedules[scheduleId] = new SchedulerWrapper
        {
            Schedule = schedule,
            JobExecutionMetadata = new(typeof(ITestJob), typeof(ITestJob).GetMethod(nameof(ITestJob.RunAsync))!, [], typeof(FakeJobLogStore))
        };

        return schedule;
    }

    #region GetJobExecutionMetadata (exercised via InsertScheduleAsync)

    [Test]
    public async Task InsertScheduleAsync_ExplicitInterfaceTypeNoArguments_ResolvesInterfaceMethod()
    {
        Expression<Func<ITestJob, Task<JobExecutionResult>>> expr = i => i.RunAsync();

        Schedule schedule = await _service.InsertScheduleAsync("Test1", false, ScheduleRecurrenceTypes.Daily, _currentDate, null, 1, null, null, false, 3, expr, typeof(ITestJob), typeof(FakeJobLogStore), _currentDate, CancellationToken.None);

        SchedulerWrapper wrapper = _scheduleStore.Schedules[schedule.ScheduleId];

        Assert.Multiple(() =>
        {
            Assert.That(wrapper.JobExecutionMetadata.JobExecutionImplementationType, Is.EqualTo(typeof(ITestJob)));
            Assert.That(wrapper.JobExecutionMetadata.Method.Name, Is.EqualTo(nameof(ITestJob.RunAsync)));
            Assert.That(wrapper.JobExecutionMetadata.Arguments, Is.Empty);
        });
    }

    [Test]
    public async Task InsertScheduleAsync_LiteralArguments_CapturesConstantValues()
    {
        Expression<Func<ITestJob, Task<JobExecutionResult>>> expr = i => i.RunWithArgsAsync(42, "hello");

        Schedule schedule = await _service.InsertScheduleAsync("Test2", false, ScheduleRecurrenceTypes.Daily, _currentDate, null, 1, null, null, false, 3, expr, typeof(ITestJob), typeof(FakeJobLogStore), _currentDate, CancellationToken.None);

        IReadOnlyList<KeyValuePair<Type, object?>> arguments = _scheduleStore.Schedules[schedule.ScheduleId].JobExecutionMetadata.Arguments;

        Assert.That(arguments.Select(a => a.Value), Is.EqualTo(new object?[] { 42, "hello" }));
    }

    [Test]
    public async Task InsertScheduleAsync_ClosureCapturedVariables_ResolvesFieldValues()
    {
        int retention = 42;
        string label = "hello";
        Expression<Func<ITestJob, Task<JobExecutionResult>>> expr = i => i.RunWithArgsAsync(retention, label);

        Schedule schedule = await _service.InsertScheduleAsync("Test3", false, ScheduleRecurrenceTypes.Daily, _currentDate, null, 1, null, null, false, 3, expr, typeof(ITestJob), typeof(FakeJobLogStore), _currentDate, CancellationToken.None);

        IReadOnlyList<KeyValuePair<Type, object?>> arguments = _scheduleStore.Schedules[schedule.ScheduleId].JobExecutionMetadata.Arguments;

        Assert.That(arguments.Select(a => a.Value), Is.EqualTo(new object?[] { 42, "hello" }));
    }

    [Test]
    public async Task InsertScheduleAsync_CapturedPropertyAccess_ResolvesPropertyValues()
    {
        TestSettings settings = new() { RetentionInSeconds = 99 };
        Expression<Func<ITestJob, Task<JobExecutionResult>>> expr = i => i.RunWithArgsAsync(settings.RetentionInSeconds, "x");

        Schedule schedule = await _service.InsertScheduleAsync("Test4", false, ScheduleRecurrenceTypes.Daily, _currentDate, null, 1, null, null, false, 3, expr, typeof(ITestJob), typeof(FakeJobLogStore), _currentDate, CancellationToken.None);

        IReadOnlyList<KeyValuePair<Type, object?>> arguments = _scheduleStore.Schedules[schedule.ScheduleId].JobExecutionMetadata.Arguments;

        Assert.That(arguments.Select(a => a.Value), Is.EqualTo(new object?[] { 99, "x" }));
    }

    [Test]
    public async Task InsertScheduleAsync_ComputedExpressionArguments_CompilesAndEvaluates()
    {
        int retention = 41;
        string label = "hello";
        Expression<Func<ITestJob, Task<JobExecutionResult>>> expr = i => i.RunWithArgsAsync(retention + 1, label.ToUpper());

        Schedule schedule = await _service.InsertScheduleAsync("Test5", false, ScheduleRecurrenceTypes.Daily, _currentDate, null, 1, null, null, false, 3, expr, typeof(ITestJob), typeof(FakeJobLogStore), _currentDate, CancellationToken.None);

        IReadOnlyList<KeyValuePair<Type, object?>> arguments = _scheduleStore.Schedules[schedule.ScheduleId].JobExecutionMetadata.Arguments;

        Assert.That(arguments.Select(a => a.Value), Is.EqualTo(new object?[] { 42, "HELLO" }));
    }

    [Test]
    public async Task InsertScheduleAsync_NoExplicitTypeWithCapturedInstance_ResolvesConcreteRuntimeType()
    {
        TestJob capturedInstance = new();
        Expression<Func<Task<JobExecutionResult>>> expr = () => capturedInstance.RunAsync();

        Schedule schedule = await _service.InsertScheduleAsync("Test6", false, ScheduleRecurrenceTypes.Daily, _currentDate, null, 1, null, null, false, 3, expr, null, typeof(FakeJobLogStore), _currentDate, CancellationToken.None);

        JobExecutionMetadata metadata = _scheduleStore.Schedules[schedule.ScheduleId].JobExecutionMetadata;

        Assert.Multiple(() =>
        {
            Assert.That(metadata.JobExecutionImplementationType, Is.EqualTo(typeof(TestJob)));
            Assert.That(metadata.Method.Name, Is.EqualTo(nameof(TestJob.RunAsync)));
        });
    }

    [Test]
    public async Task InsertScheduleAsync_NoExplicitTypeStaticMethodCall_ResolvesDeclaringType()
    {
        Expression<Func<JobExecutionResult>> expr = () => TestStaticJob.RunStatic();

        Schedule schedule = await _service.InsertScheduleAsync("Test7", false, ScheduleRecurrenceTypes.Daily, _currentDate, null, 1, null, null, false, 3, expr, null, typeof(FakeJobLogStore), _currentDate, CancellationToken.None);

        JobExecutionMetadata metadata = _scheduleStore.Schedules[schedule.ScheduleId].JobExecutionMetadata;

        Assert.Multiple(() =>
        {
            Assert.That(metadata.JobExecutionImplementationType, Is.EqualTo(typeof(TestStaticJob)));
            Assert.That(metadata.Method.Name, Is.EqualTo(nameof(TestStaticJob.RunStatic)));
        });
    }

    [Test]
    public void InsertScheduleAsync_ExpressionBodyIsNotAMethodCall_ThrowsArgumentException()
    {
        Expression<Func<JobExecutionResult?>> expr = () => null;

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.InsertScheduleAsync("Test8", false, ScheduleRecurrenceTypes.Daily, _currentDate, null, 1, null, null, false, 3, expr, null, typeof(FakeJobLogStore), _currentDate, CancellationToken.None));
    }

    #endregion

    #region Insert

    [Test]
    public async Task InsertScheduleAsync_SetsExpectedScheduleFields()
    {
        Expression<Func<ITestJob, Task<JobExecutionResult>>> expr = i => i.RunAsync();

        Schedule schedule = await _service.InsertScheduleAsync("MySchedule", true, ScheduleRecurrenceTypes.Daily, _currentDate.AddDays(1), null, 1, null, null, true, 5, expr, typeof(ITestJob), typeof(FakeJobLogStore), _currentDate, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(schedule.ScheduleName, Is.EqualTo("MySchedule"));
            Assert.That(schedule.IsSystem, Is.True);
            Assert.That(schedule.IsActive, Is.True);
            Assert.That(schedule.CreationDate, Is.EqualTo(_currentDate));
            Assert.That(schedule.ScheduleExecutionStatus, Is.EqualTo(ScheduleExecutionStatuses.Ready));
            Assert.That(schedule.LastOccurrenceStatus, Is.EqualTo(ScheduleOccurrenceStatuses.Never));
            Assert.That(schedule.NextOccurrenceDate, Is.EqualTo(_currentDate.AddDays(1))); // future start date -> occurrence is the start date itself
            Assert.That(schedule.ScheduleRetryConfig.AutoRestartOnFailure, Is.True);
            Assert.That(schedule.ScheduleRetryConfig.RetryLimit, Is.EqualTo((byte)5));
        });
    }

    #endregion

    #region Update

    [Test]
    public async Task UpdateScheduleNameAsync_HappyPath_UpdatesName()
    {
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId);

        Schedule updated = await _service.UpdateScheduleNameAsync(scheduleId, "Renamed", _currentDate, CancellationToken.None);

        Assert.That(updated.ScheduleName, Is.EqualTo("Renamed"));
    }

    [Test]
    public void UpdateScheduleNameAsync_ScheduleNotFound_ThrowsKeyNotFoundException()
        => Assert.ThrowsAsync<KeyNotFoundException>(async () => await _service.UpdateScheduleNameAsync(Guid.NewGuid(), "Renamed", _currentDate, CancellationToken.None));

    [Test]
    public async Task UpdateScheduleStatusAsync_ActivatingInactiveSchedule_RecalculatesNextOccurrenceDate()
    {
        Guid scheduleId = Guid.NewGuid();
        DateTime futureStart = _currentDate.AddDays(5);
        Schedule schedule = SeedSchedule(scheduleId, ScheduleRecurrenceTypes.Daily, 1, startDate: futureStart, nextOccurrenceDate: null, isActive: false);

        Schedule updated = await _service.UpdateScheduleStatusAsync(schedule, isActive: true, _currentDate, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(updated.IsActive, Is.True);
            Assert.That(updated.NextOccurrenceDate, Is.EqualTo(futureStart));
        });
    }

    [Test]
    public async Task UpdateScheduleStatusAsync_Deactivating_DoesNotRecalculateNextOccurrenceDate()
    {
        Guid scheduleId = Guid.NewGuid();
        DateTime existingNextOccurrence = _currentDate.AddDays(2);
        Schedule schedule = SeedSchedule(scheduleId, nextOccurrenceDate: existingNextOccurrence, isActive: true);

        Schedule updated = await _service.UpdateScheduleStatusAsync(schedule, isActive: false, _currentDate, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(updated.IsActive, Is.False);
            Assert.That(updated.NextOccurrenceDate, Is.EqualTo(existingNextOccurrence));
        });
    }

    [Test]
    public async Task UpdateScheduleRetryConfigsAsync_HappyPath_UpdatesRetryConfig()
    {
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId);

        Schedule updated = await _service.UpdateScheduleRetryConfigsAsync(scheduleId, true, 7, _currentDate, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(updated.ScheduleRetryConfig.AutoRestartOnFailure, Is.True);
            Assert.That(updated.ScheduleRetryConfig.RetryLimit, Is.EqualTo((byte)7));
        });
    }

    [TestCase((byte)0, null)]
    [TestCase((byte)1, 1)]
    [TestCase((byte)3, 3)]
    public async Task UpdateScheduleRetryCountAsync_NextRetryDateDerivedFromRetryCount(byte retryCount, int? expectedMinutesFromNow)
    {
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId);

        Schedule updated = await _service.UpdateScheduleRetryCountAsync(scheduleId, retryCount, _currentDate, CancellationToken.None);

        DateTime? expected = expectedMinutesFromNow is null ? null : _currentDate.AddMinutes(expectedMinutesFromNow.Value);

        Assert.Multiple(() =>
        {
            Assert.That(updated.ScheduleRetryConfig.RetryCount, Is.EqualTo(retryCount));
            Assert.That(updated.ScheduleRetryConfig.NextRetryDate, Is.EqualTo(expected));
        });
    }

    [Test]
    public async Task UpdateScheduleNextOccurrenceDateAsync_UnchangedOccurrence_DoesNotCallStore()
    {
        // Deliberately not seeded in the store: if the store update were invoked, FakeScheduleStore
        // would return null (schedule not found) and this would throw KeyNotFoundException instead.
        DateTime futureStart = _currentDate.AddDays(3);
        Schedule schedule = new()
        {
            ScheduleId = Guid.NewGuid(),
            ScheduleName = "Untouched",
            IsSystem = false,
            IsActive = true,
            ScheduleExecutionStatus = ScheduleExecutionStatuses.Ready,
            LastOccurrenceStatus = ScheduleOccurrenceStatuses.Never,
            CreationDate = _currentDate,
            NextOccurrenceDate = futureStart, // already equal to what recalculation will produce
            ScheduleRecurrenceConfig = new() { ScheduleRecurrenceType = ScheduleRecurrenceTypes.Daily, StartDate = futureStart, EveryX = 1 },
            ScheduleRetryConfig = new()
        };

        Schedule result = await _service.UpdateScheduleNextOccurrenceDateAsync(schedule, _currentDate, CancellationToken.None);

        Assert.That(result, Is.SameAs(schedule));
    }

    [Test]
    public async Task UpdateScheduleNextOccurrenceDateAsync_ChangedOccurrence_UpdatesStore()
    {
        Guid scheduleId = Guid.NewGuid();
        DateTime pastNextOccurrence = _currentDate.AddDays(-1);
        Schedule schedule = SeedSchedule(scheduleId, ScheduleRecurrenceTypes.Daily, 1, startDate: _currentDate.AddDays(-10), nextOccurrenceDate: pastNextOccurrence);

        Schedule result = await _service.UpdateScheduleNextOccurrenceDateAsync(schedule, _currentDate, CancellationToken.None);

        Assert.That(result.NextOccurrenceDate, Is.Not.EqualTo(pastNextOccurrence));
    }

    [Test]
    public async Task UpdateScheduleJobExecutionMetadataAsync_UpdatesMetadataForFoundSchedules()
    {
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId);

        Expression<Func<ITestJob, Task<JobExecutionResult>>> expr = i => i.RunWithArgsAsync(1, "a");

        await _service.UpdateScheduleJobExecutionMetadataAsync(
            new Dictionary<Guid, (LambdaExpression methodCall, Type? explicitType, Type jobLogStoreImplementationType)>
            {
                [scheduleId] = (expr, typeof(ITestJob), typeof(FakeJobLogStore))
            },
            _currentDate,
            CancellationToken.None);

        Assert.That(_scheduleStore.Schedules[scheduleId].JobExecutionMetadata.Method.Name, Is.EqualTo(nameof(ITestJob.RunWithArgsAsync)));
    }

    [Test]
    public void UpdateScheduleJobExecutionMetadataAsync_MissingSchedule_ThrowsKeyNotFoundException()
    {
        Expression<Func<ITestJob, Task<JobExecutionResult>>> expr = i => i.RunAsync();

        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _service.UpdateScheduleJobExecutionMetadataAsync(
            new Dictionary<Guid, (LambdaExpression methodCall, Type? explicitType, Type jobLogStoreImplementationType)>
            {
                [Guid.NewGuid()] = (expr, typeof(ITestJob), typeof(FakeJobLogStore))
            },
            _currentDate,
            CancellationToken.None));
    }

    [Test]
    public async Task UpdateSchedulesExecutionStatusAsync_AllFound_ReturnsUpdatedSchedules()
    {
        Guid scheduleId1 = Guid.NewGuid();
        Guid scheduleId2 = Guid.NewGuid();
        SeedSchedule(scheduleId1);
        SeedSchedule(scheduleId2);

        IEnumerable<Schedule> result = await _service.UpdateSchedulesExecutionStatusAsync([scheduleId1, scheduleId2], ScheduleExecutionStatuses.Queued, _currentDate, CancellationToken.None);

        Assert.That(result.Select(s => s.ScheduleExecutionStatus), Is.All.EqualTo(ScheduleExecutionStatuses.Queued));
    }

    [Test]
    public void UpdateSchedulesExecutionStatusAsync_SomeMissing_ThrowsKeyNotFoundException()
    {
        Guid found = Guid.NewGuid();
        SeedSchedule(found);
        Guid missing = Guid.NewGuid();

        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _service.UpdateSchedulesExecutionStatusAsync([found, missing], ScheduleExecutionStatuses.Queued, _currentDate, CancellationToken.None));
    }

    #endregion

    #region Delete

    [Test]
    public async Task DeleteSchedulesAsync_RemovesFromStore()
    {
        Guid scheduleId = Guid.NewGuid();
        SeedSchedule(scheduleId);

        await _service.DeleteSchedulesAsync([scheduleId], CancellationToken.None);

        Assert.That(_scheduleStore.Schedules.ContainsKey(scheduleId), Is.False);
    }

    #endregion
}
