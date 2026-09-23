using Bacon.Scheduler.Interfaces.Services.Batches;
using Bacon.Scheduler.Interfaces.Services.Jobs;
using Bacon.Scheduler.Interfaces.Services.Queues;
using Bacon.Scheduler.Interfaces.Services.Schedules;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Models.Schedules;
using Bacon.Scheduler.Services.Batches;
using Bacon.Scheduler.Services.Jobs;
using Bacon.Scheduler.Services.Schedules;
using Bacon.Scheduler.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Bacon.Scheduler.Tests.Services.Schedules;

/// <summary>
/// Exercises SchedulerHostedService.InitSystemSchedules - the reconciliation that runs once at
/// start-up to insert new system schedules, update changed ones, and delete orphaned ones. The
/// method is private (it's only ever invoked from BackgroundService.ExecuteAsync), so these tests
/// invoke it directly via reflection rather than going through the BackgroundService lifecycle,
/// which would otherwise require racing an unawaited background Task.
/// </summary>
[TestFixture]
public class SchedulerHostedServiceSystemScheduleTests
{
    private static readonly Expression<Func<ITestJob, Task<JobExecutionResult>>> _methodCall = i => i.RunAsync();

    private FakeScheduleStore _scheduleStore = null!;
    private FakeTimeProvider _timeProvider = null!;
    private DateTime _currentDate;

    [SetUp]
    public void Setup()
    {
        _scheduleStore = new FakeScheduleStore();
        _currentDate = new DateTime(2026, 7, 30, 8, 0, 0, DateTimeKind.Utc);
        _timeProvider = new FakeTimeProvider(_currentDate);
    }

    private static SystemSchedule BuildSystemSchedule(string name, bool isActive = true, ScheduleRecurrenceTypes recurrenceType = ScheduleRecurrenceTypes.Daily, short everyX = 1
        , IReadOnlyCollection<Weekdays>? weekdays = null, IReadOnlyCollection<string>? dayOfTheMonths = null, bool autoRestartOnFailure = false, byte retryLimit = 3)
        => new(name, isActive, recurrenceType, new TimeOnly(1, 0, 0), everyX, weekdays, dayOfTheMonths, autoRestartOnFailure, retryLimit, _methodCall, typeof(ITestJob), typeof(FakeJobLogStore));

    private async Task<SchedulerHostedService> RunInitSystemSchedulesAsync(Dictionary<string, SystemSchedule> systemSchedules)
    {
        InternalScheduleService internalScheduleService = new(_scheduleStore, new SchedulerOccurrenceService(), NullLogger<InternalScheduleService>.Instance);
        ScheduleValidationService scheduleValidationService = new(_scheduleStore);
        BatchService batchService = new(new FakeBatchStore());
        JobService jobService = new(new FakeJobStore());

        SchedulerHostedService hostedService = new(
            new FakeQueueHandlerService(),
            internalScheduleService,
            scheduleValidationService,
            batchService,
            jobService,
            Options.Create(new SchedulerOptions()),
            systemSchedules,
            _timeProvider,
            NullLogger<SchedulerHostedService>.Instance);

        MethodInfo initMethod = typeof(SchedulerHostedService).GetMethod("InitSystemSchedules", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Task task = (Task)initMethod.Invoke(hostedService, [CancellationToken.None])!;
        await task;

        return hostedService;
    }

    [Test]
    public async Task NewSystemSchedule_IsInsertedWithSystemFlagAndJobExecutionMetadata()
    {
        Dictionary<string, SystemSchedule> systemSchedules = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Maintenance"] = BuildSystemSchedule("Maintenance", isActive: true, ScheduleRecurrenceTypes.Weekly, 1, [Weekdays.Monday])
        };

        await RunInitSystemSchedulesAsync(systemSchedules);

        Assert.That(_scheduleStore.Schedules.Values, Has.Count.EqualTo(1));

        SchedulerWrapper wrapper = _scheduleStore.Schedules.Values.Single();

        Assert.Multiple(() =>
        {
            Assert.That(wrapper.Schedule.ScheduleName, Is.EqualTo("Maintenance"));
            Assert.That(wrapper.Schedule.IsSystem, Is.True);
            Assert.That(wrapper.Schedule.IsActive, Is.True);
            Assert.That(wrapper.Schedule.ScheduleRecurrenceConfig.ScheduleRecurrenceType, Is.EqualTo(ScheduleRecurrenceTypes.Weekly));
            Assert.That(wrapper.JobExecutionMetadata.JobExecutionImplementationType, Is.EqualTo(typeof(ITestJob)));
            Assert.That(wrapper.JobExecutionMetadata.Method.Name, Is.EqualTo(nameof(ITestJob.RunAsync)));
            Assert.That(wrapper.JobExecutionMetadata.JobLogStoreImplementationType, Is.EqualTo(typeof(FakeJobLogStore)));
        });
    }

    [Test]
    public async Task ChangedSystemSchedule_IsUpdatedInPlace()
    {
        Guid scheduleId = Guid.NewGuid();
        _scheduleStore.Schedules[scheduleId] = new SchedulerWrapper
        {
            Schedule = new Schedule
            {
                ScheduleId = scheduleId,
                ScheduleName = "Maintenance",
                IsSystem = true,
                IsActive = false,
                ScheduleExecutionStatus = ScheduleExecutionStatuses.Ready,
                LastOccurrenceStatus = ScheduleOccurrenceStatuses.Never,
                CreationDate = _currentDate.AddDays(-30),
                ScheduleRecurrenceConfig = new()
                {
                    ScheduleRecurrenceType = ScheduleRecurrenceTypes.Daily,
                    StartDate = _currentDate.AddDays(-30),
                    EveryX = 1
                },
                ScheduleRetryConfig = new()
                {
                    AutoRestartOnFailure = false,
                    RetryLimit = 1
                }
            },
            JobExecutionMetadata = new(typeof(ITestJob), typeof(ITestJob).GetMethod(nameof(ITestJob.RunAsync))!, [], typeof(FakeJobLogStore))
        };

        Dictionary<string, SystemSchedule> systemSchedules = new(StringComparer.OrdinalIgnoreCase)
        {
            // isActive flips true, retryLimit changes from 1 to 5 - both should be picked up as changes
            ["Maintenance"] = BuildSystemSchedule("Maintenance", isActive: true, retryLimit: 5)
        };

        await RunInitSystemSchedulesAsync(systemSchedules);

        Schedule updated = _scheduleStore.Schedules[scheduleId].Schedule;

        Assert.Multiple(() =>
        {
            Assert.That(updated.IsActive, Is.True);
            Assert.That(updated.ScheduleRetryConfig.RetryLimit, Is.EqualTo((byte)5));
            Assert.That(updated.ModificationDate, Is.EqualTo(_currentDate));
        });
    }

    [Test]
    public async Task UnchangedSystemSchedule_IsNotTouched()
    {
        Guid scheduleId = Guid.NewGuid();
        _scheduleStore.Schedules[scheduleId] = new SchedulerWrapper
        {
            Schedule = new Schedule
            {
                ScheduleId = scheduleId,
                ScheduleName = "Maintenance",
                IsSystem = true,
                IsActive = true,
                ScheduleExecutionStatus = ScheduleExecutionStatuses.Ready,
                LastOccurrenceStatus = ScheduleOccurrenceStatuses.Never,
                CreationDate = _currentDate.AddDays(-30),
                ModificationDate = null,
                ScheduleRecurrenceConfig = new()
                {
                    ScheduleRecurrenceType = ScheduleRecurrenceTypes.Daily,
                    StartDate = _currentDate.AddDays(-30),
                    EveryX = 1
                },
                ScheduleRetryConfig = new()
                {
                    AutoRestartOnFailure = false,
                    RetryLimit = 3
                }
            },
            JobExecutionMetadata = new(typeof(ITestJob), typeof(ITestJob).GetMethod(nameof(ITestJob.RunAsync))!, [], typeof(FakeJobLogStore))
        };

        Dictionary<string, SystemSchedule> systemSchedules = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Maintenance"] = BuildSystemSchedule("Maintenance", isActive: true, retryLimit: 3)
        };

        await RunInitSystemSchedulesAsync(systemSchedules);

        // None of the per-field update calls should have fired, so ModificationDate (only ever set by an update call) stays null.
        Assert.That(_scheduleStore.Schedules[scheduleId].Schedule.ModificationDate, Is.Null);
    }

    [Test]
    public async Task OrphanedSystemSchedule_IsDeleted()
    {
        Guid scheduleId = Guid.NewGuid();
        _scheduleStore.Schedules[scheduleId] = new SchedulerWrapper
        {
            Schedule = new Schedule
            {
                ScheduleId = scheduleId,
                ScheduleName = "NoLongerRegistered",
                IsSystem = true,
                IsActive = true,
                ScheduleExecutionStatus = ScheduleExecutionStatuses.Ready,
                LastOccurrenceStatus = ScheduleOccurrenceStatuses.Never,
                CreationDate = _currentDate.AddDays(-30),
                ScheduleRecurrenceConfig = new()
                {
                    ScheduleRecurrenceType = ScheduleRecurrenceTypes.Daily,
                    StartDate = _currentDate.AddDays(-30),
                    EveryX = 1
                },
                ScheduleRetryConfig = new()
                {
                    AutoRestartOnFailure = false,
                    RetryLimit = 3
                }
            },
            JobExecutionMetadata = new(typeof(ITestJob), typeof(ITestJob).GetMethod(nameof(ITestJob.RunAsync))!, [], typeof(FakeJobLogStore))
        };

        // Empty: nothing in code registers "NoLongerRegistered" anymore.
        await RunInitSystemSchedulesAsync(new Dictionary<string, SystemSchedule>(StringComparer.OrdinalIgnoreCase));

        Assert.That(_scheduleStore.Schedules.ContainsKey(scheduleId), Is.False);
    }

    [Test]
    public async Task SystemScheduleNameCollidesWithExistingNonSystemSchedule_ThrowsAggregateExceptionWithDuplicateNameException()
    {
        Guid scheduleId = Guid.NewGuid();
        _scheduleStore.Schedules[scheduleId] = new SchedulerWrapper
        {
            Schedule = new Schedule
            {
                ScheduleId = scheduleId,
                ScheduleName = "Maintenance",
                IsSystem = false, // a dynamic (non-system) schedule already owns this name
                IsActive = true,
                ScheduleExecutionStatus = ScheduleExecutionStatuses.Ready,
                LastOccurrenceStatus = ScheduleOccurrenceStatuses.Never,
                CreationDate = _currentDate.AddDays(-30),
                ScheduleRecurrenceConfig = new()
                {
                    ScheduleRecurrenceType = ScheduleRecurrenceTypes.Daily,
                    StartDate = _currentDate.AddDays(-30),
                    EveryX = 1
                },
                ScheduleRetryConfig = new()
                {
                    AutoRestartOnFailure = false,
                    RetryLimit = 3
                }
            },
            JobExecutionMetadata = new(typeof(ITestJob), typeof(ITestJob).GetMethod(nameof(ITestJob.RunAsync))!, [], typeof(FakeJobLogStore))
        };

        Dictionary<string, SystemSchedule> systemSchedules = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Maintenance"] = BuildSystemSchedule("Maintenance")
        };

        AggregateException aggregateException = Assert.ThrowsAsync<AggregateException>(async () => await RunInitSystemSchedulesAsync(systemSchedules))!;

        Assert.That(aggregateException.InnerExceptions, Has.Some.InstanceOf<DuplicateNameException>());
    }

    [Test]
    public async Task InvalidRetryLimit_ThrowsAggregateExceptionButStillInsertsTheSchedule()
    {
        // ValidateSystemSchedules runs independently of, and does not gate, the insert/update fan-out:
        // a validation failure is reported (aggregated and thrown) but the schedule is written anyway.
        Dictionary<string, SystemSchedule> systemSchedules = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Maintenance"] = BuildSystemSchedule("Maintenance", retryLimit: 50) // ValidateScheduleRetry only allows 0-10
        };

        AggregateException aggregateException = Assert.ThrowsAsync<AggregateException>(async () => await RunInitSystemSchedulesAsync(systemSchedules))!;

        Assert.Multiple(() =>
        {
            Assert.That(aggregateException.InnerExceptions, Has.Some.InstanceOf<ArgumentException>());
            Assert.That(_scheduleStore.Schedules.Values.Single().Schedule.ScheduleRetryConfig.RetryLimit, Is.EqualTo((byte)50));
        });
    }
}
