using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Schedules;
using Bacon.Scheduler.Services.Schedules;
using Bacon.Scheduler.Tests.Fakes;
using System.Data;

namespace Bacon.Scheduler.Tests.Services.Schedules;

[TestFixture]
public class ScheduleValidationServiceTests
{
    private FakeScheduleStore _scheduleStore = null!;
    private ScheduleValidationService _service = null!;

    [SetUp]
    public void Setup()
    {
        _scheduleStore = new FakeScheduleStore();
        _service = new ScheduleValidationService(_scheduleStore);
    }

    private static Schedule BuildSchedule(bool isSystem) => new()
    {
        ScheduleId = Guid.NewGuid(),
        ScheduleName = "Test",
        IsSystem = isSystem,
        IsActive = true,
        ScheduleExecutionStatus = ScheduleExecutionStatuses.Ready,
        LastOccurrenceStatus = ScheduleOccurrenceStatuses.Never,
        CreationDate = DateTime.UtcNow,
        ScheduleRecurrenceConfig = new() { ScheduleRecurrenceType = ScheduleRecurrenceTypes.Daily, StartDate = DateTime.UtcNow, EveryX = 1 },
        ScheduleRetryConfig = new()
    };

    #region ValidateSystemSchedules

    [Test]
    public void ValidateSystemSchedules_EmptyCollection_DoesNotThrow()
        => Assert.DoesNotThrow(() => _service.ValidateSystemSchedules([]));

    [Test]
    public void ValidateSystemSchedules_AllNonSystem_DoesNotThrow()
        => Assert.DoesNotThrow(() => _service.ValidateSystemSchedules([BuildSchedule(false), BuildSchedule(false)]));

    [Test]
    public void ValidateSystemSchedules_ContainsSystemSchedule_ThrowsInvalidOperation()
        => Assert.Throws<InvalidOperationException>(() => _service.ValidateSystemSchedules([BuildSchedule(false), BuildSchedule(true)]));

    #endregion

    #region ValidateScheduleRetry

    [TestCase((byte)0)]
    [TestCase((byte)10)]
    public void ValidateScheduleRetry_WithinRange_DoesNotThrow(byte retryLimit)
        => Assert.DoesNotThrow(() => _service.ValidateScheduleRetry("id", retryLimit));

    [TestCase((byte)11)]
    [TestCase((byte)255)]
    public void ValidateScheduleRetry_AboveRange_ThrowsArgumentException(byte retryLimit)
        => Assert.Throws<ArgumentException>(() => _service.ValidateScheduleRetry("id", retryLimit));

    #endregion

    #region ValidateUtcDates

    [Test]
    public void ValidateUtcDates_BothNull_DoesNotThrow()
        => Assert.DoesNotThrow(() => _service.ValidateUtcDates(null, null));

    [Test]
    public void ValidateUtcDates_BothUtc_DoesNotThrow()
        => Assert.DoesNotThrow(() => _service.ValidateUtcDates(DateTime.UtcNow, DateTime.UtcNow.AddDays(1)));

    [Test]
    public void ValidateUtcDates_StartDateNotUtc_ThrowsArgumentException()
        => Assert.Throws<ArgumentException>(() => _service.ValidateUtcDates(DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local), null));

    [Test]
    public void ValidateUtcDates_StartDateUnspecifiedKind_ThrowsArgumentException()
        => Assert.Throws<ArgumentException>(() => _service.ValidateUtcDates(DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified), null));

    [Test]
    public void ValidateUtcDates_EndDateNotUtc_ThrowsArgumentException()
        => Assert.Throws<ArgumentException>(() => _service.ValidateUtcDates(DateTime.UtcNow, DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local)));

    #endregion

    #region ValidateStartAndEndDate

    [Test]
    public void ValidateStartAndEndDate_StartDateEqualsCurrentDate_DoesNotThrow()
    {
        DateTime now = DateTime.UtcNow;
        Assert.DoesNotThrow(() => _service.ValidateStartAndEndDate(now, null, now));
    }

    [Test]
    public void ValidateStartAndEndDate_StartDateBeforeCurrentDate_ThrowsArgumentOutOfRange()
    {
        DateTime now = DateTime.UtcNow;
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.ValidateStartAndEndDate(now.AddMinutes(-1), null, now));
    }

    [Test]
    public void ValidateStartAndEndDate_EndDateEqualsStartDate_DoesNotThrow()
    {
        DateTime start = DateTime.UtcNow.AddDays(1);
        Assert.DoesNotThrow(() => _service.ValidateStartAndEndDate(start, start, DateTime.UtcNow));
    }

    [Test]
    public void ValidateStartAndEndDate_EndDateBeforeStartDate_ThrowsArgumentOutOfRange()
    {
        DateTime start = DateTime.UtcNow.AddDays(2);
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.ValidateStartAndEndDate(start, start.AddDays(-1), DateTime.UtcNow));
    }

    #endregion

    #region ValidateScheduleRecurrence

    [Test]
    public void ValidateScheduleRecurrence_HourlyWithinFrequency_DoesNotThrow()
        => Assert.DoesNotThrow(() => _service.ValidateScheduleRecurrence("id", ScheduleRecurrenceTypes.Hourly, 12, null, null));

    [Test]
    public void ValidateScheduleRecurrence_HourlyFrequencyOutOfRange_ThrowsArgumentException()
        => Assert.Throws<ArgumentException>(() => _service.ValidateScheduleRecurrence("id", ScheduleRecurrenceTypes.Hourly, 25, null, null));

    [Test]
    public void ValidateScheduleRecurrence_HourlyWithWeekdays_ThrowsArgumentException()
        => Assert.Throws<ArgumentException>(() => _service.ValidateScheduleRecurrence("id", ScheduleRecurrenceTypes.Hourly, 1, [Weekdays.Monday], null));

    [Test]
    public void ValidateScheduleRecurrence_HourlyWithEmptyWeekdaysCollection_DoesNotThrow()
        => Assert.DoesNotThrow(() => _service.ValidateScheduleRecurrence("id", ScheduleRecurrenceTypes.Hourly, 1, [], null));

    [Test]
    public void ValidateScheduleRecurrence_HourlyWithDaysOfTheMonth_ThrowsArgumentException()
        => Assert.Throws<ArgumentException>(() => _service.ValidateScheduleRecurrence("id", ScheduleRecurrenceTypes.Hourly, 1, null, ["1"]));

    [Test]
    public void ValidateScheduleRecurrence_WeeklyWithWeekdays_DoesNotThrow()
        => Assert.DoesNotThrow(() => _service.ValidateScheduleRecurrence("id", ScheduleRecurrenceTypes.Weekly, 1, [Weekdays.Monday], null));

    [Test]
    public void ValidateScheduleRecurrence_WeeklyWithDaysOfTheMonth_ThrowsArgumentException()
        => Assert.Throws<ArgumentException>(() => _service.ValidateScheduleRecurrence("id", ScheduleRecurrenceTypes.Weekly, 1, null, ["1"]));

    [Test]
    public void ValidateScheduleRecurrence_MonthlyWithDaysOfTheMonth_DoesNotThrow()
        => Assert.DoesNotThrow(() => _service.ValidateScheduleRecurrence("id", ScheduleRecurrenceTypes.Monthly, 1, null, ["1", "L"]));

    [Test]
    public void ValidateScheduleRecurrence_MonthlyWithWeekdays_ThrowsArgumentException()
        => Assert.Throws<ArgumentException>(() => _service.ValidateScheduleRecurrence("id", ScheduleRecurrenceTypes.Monthly, 1, [Weekdays.Monday], null));

    #endregion

    #region ValidateScheduleFrequency

    [TestCase(ScheduleRecurrenceTypes.Hourly, (short)1)]
    [TestCase(ScheduleRecurrenceTypes.Hourly, (short)24)]
    [TestCase(ScheduleRecurrenceTypes.Daily, (short)1)]
    [TestCase(ScheduleRecurrenceTypes.Daily, (short)365)]
    [TestCase(ScheduleRecurrenceTypes.Weekly, (short)1)]
    [TestCase(ScheduleRecurrenceTypes.Weekly, (short)52)]
    [TestCase(ScheduleRecurrenceTypes.Monthly, (short)1)]
    [TestCase(ScheduleRecurrenceTypes.Monthly, (short)12)]
    public void ValidateScheduleFrequency_BoundaryValues_DoesNotThrow(ScheduleRecurrenceTypes recurrenceType, short everyX)
        => Assert.DoesNotThrow(() => _service.ValidateScheduleFrequency(recurrenceType, everyX));

    [TestCase(ScheduleRecurrenceTypes.Hourly, (short)0)]
    [TestCase(ScheduleRecurrenceTypes.Hourly, (short)25)]
    [TestCase(ScheduleRecurrenceTypes.Daily, (short)0)]
    [TestCase(ScheduleRecurrenceTypes.Daily, (short)366)]
    [TestCase(ScheduleRecurrenceTypes.Weekly, (short)0)]
    [TestCase(ScheduleRecurrenceTypes.Weekly, (short)53)]
    [TestCase(ScheduleRecurrenceTypes.Monthly, (short)0)]
    [TestCase(ScheduleRecurrenceTypes.Monthly, (short)13)]
    public void ValidateScheduleFrequency_OutOfRangeValues_ThrowsArgumentException(ScheduleRecurrenceTypes recurrenceType, short everyX)
        => Assert.Throws<ArgumentException>(() => _service.ValidateScheduleFrequency(recurrenceType, everyX));

    [Test]
    public void ValidateScheduleFrequency_UnmappedRecurrenceType_ThrowsInvalidOperation()
        => Assert.Throws<InvalidOperationException>(() => _service.ValidateScheduleFrequency((ScheduleRecurrenceTypes)99, 1));

    #endregion

    #region ValidateMonthlyDayOfTheMonths

    [Test]
    public void ValidateMonthlyDayOfTheMonths_NonMonthlyType_DoesNotThrowEvenWithGarbageInput()
        => Assert.DoesNotThrow(() => _service.ValidateMonthlyDayOfTheMonths("id", ScheduleRecurrenceTypes.Daily, ["garbage", "999", null!]));

    [Test]
    public void ValidateMonthlyDayOfTheMonths_MonthlyWithNull_DoesNotThrow()
        => Assert.DoesNotThrow(() => _service.ValidateMonthlyDayOfTheMonths("id", ScheduleRecurrenceTypes.Monthly, null));

    [Test]
    public void ValidateMonthlyDayOfTheMonths_MonthlyWithEmptyCollection_DoesNotThrow()
        => Assert.DoesNotThrow(() => _service.ValidateMonthlyDayOfTheMonths("id", ScheduleRecurrenceTypes.Monthly, []));

    [Test]
    public void ValidateMonthlyDayOfTheMonths_ValidDaysAndLastDayMarker_DoesNotThrow()
        => Assert.DoesNotThrow(() => _service.ValidateMonthlyDayOfTheMonths("id", ScheduleRecurrenceTypes.Monthly, ["1", "15", "31", "L", "l"]));

    [TestCase("")]
    [TestCase(null)]
    public void ValidateMonthlyDayOfTheMonths_NullOrEmptyDayValue_ThrowsArgumentException(string? day)
        => Assert.Throws<ArgumentException>(() => _service.ValidateMonthlyDayOfTheMonths("id", ScheduleRecurrenceTypes.Monthly, [day!]));

    [Test]
    public void ValidateMonthlyDayOfTheMonths_LetterOtherThanL_ThrowsArgumentException()
        => Assert.Throws<ArgumentException>(() => _service.ValidateMonthlyDayOfTheMonths("id", ScheduleRecurrenceTypes.Monthly, ["X"]));

    [TestCase("0")]
    [TestCase("32")]
    public void ValidateMonthlyDayOfTheMonths_NumericOutOfByteRange_ThrowsArgumentException(string day)
        => Assert.Throws<ArgumentException>(() => _service.ValidateMonthlyDayOfTheMonths("id", ScheduleRecurrenceTypes.Monthly, [day]));

    [Test]
    public void ValidateMonthlyDayOfTheMonths_ValueTooLargeToParseAsByte_KnownGap_DoesNotThrow()
    {
        Assert.Throws<ArgumentException>(() => _service.ValidateMonthlyDayOfTheMonths("id", ScheduleRecurrenceTypes.Monthly, ["300"]));
    }

    #endregion

    #region ValidateImpossibleRecurrence

    [Test]
    public void ValidateImpossibleRecurrence_NonMonthlyType_DoesNotThrow()
        => Assert.DoesNotThrow(() => _service.ValidateImpossibleRecurrence("id", ScheduleRecurrenceTypes.Daily, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), 12, ["30"]));

    [Test]
    public void ValidateImpossibleRecurrence_MonthlyEveryXNotTwelve_DoesNotThrowEvenForImpossibleDay()
        => Assert.DoesNotThrow(() => _service.ValidateImpossibleRecurrence("id", ScheduleRecurrenceTypes.Monthly, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), 1, ["30"]));

    [Test]
    public void ValidateImpossibleRecurrence_FebruaryWithOnlyDay30And31_ThrowsArgumentException()
        => Assert.Throws<ArgumentException>(() => _service.ValidateImpossibleRecurrence("id", ScheduleRecurrenceTypes.Monthly, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), 12, ["30", "31"]));

    [Test]
    public void ValidateImpossibleRecurrence_FebruaryWithDay28AlongsideDay30_DoesNotThrow()
        => Assert.DoesNotThrow(() => _service.ValidateImpossibleRecurrence("id", ScheduleRecurrenceTypes.Monthly, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), 12, ["28", "30"]));

    [Test]
    public void ValidateImpossibleRecurrence_FebruaryWithLastDayMarker_DoesNotThrow()
        => Assert.DoesNotThrow(() => _service.ValidateImpossibleRecurrence("id", ScheduleRecurrenceTypes.Monthly, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), 12, ["L"]));

    [Test]
    public void ValidateImpossibleRecurrence_ThirtyDayMonthWithOnlyDay31_ThrowsArgumentException()
        => Assert.Throws<ArgumentException>(() => _service.ValidateImpossibleRecurrence("id", ScheduleRecurrenceTypes.Monthly, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), 12, ["31"]));

    [Test]
    public void ValidateImpossibleRecurrence_ThirtyDayMonthWithDay30AlongsideDay31_DoesNotThrow()
        => Assert.DoesNotThrow(() => _service.ValidateImpossibleRecurrence("id", ScheduleRecurrenceTypes.Monthly, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), 12, ["30", "31"]));

    [Test]
    public void ValidateImpossibleRecurrence_ThirtyOneDayMonthWithOnlyDay31_DoesNotThrow()
        => Assert.DoesNotThrow(() => _service.ValidateImpossibleRecurrence("id", ScheduleRecurrenceTypes.Monthly, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 12, ["31"]));

    [Test]
    public void ValidateImpossibleRecurrence_ThirtyDayMonthWithNullDayOfTheMonths_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _service.ValidateImpossibleRecurrence("id", ScheduleRecurrenceTypes.Monthly, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), 12, null));
    }

    #endregion

    #region ValidateScheduleNameExist

    [Test]
    public async Task ValidateScheduleNameExist_NameDoesNotExist_DoesNotThrow()
    {
        await _service.ValidateScheduleNameExist(null, "NewName", CancellationToken.None);
    }

    [Test]
    public void ValidateScheduleNameExist_NameAlreadyExists_ThrowsDuplicateNameException()
    {
        Schedule schedule = BuildSchedule(isSystem: false);
        schedule.ScheduleName = "Existing";

        SchedulerWrapper wrapper = new()
        {
            Schedule = schedule,
            JobExecutionMetadata = new(typeof(object), typeof(object).GetMethod(nameof(ToString))!, [], typeof(FakeJobLogStore))
        };
        _scheduleStore.Schedules[wrapper.Schedule.ScheduleId] = wrapper;

        Assert.ThrowsAsync<DuplicateNameException>(async () => await _service.ValidateScheduleNameExist(null, "Existing", CancellationToken.None));
    }

    #endregion
}
