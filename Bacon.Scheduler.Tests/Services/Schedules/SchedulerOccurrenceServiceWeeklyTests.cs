using Bacon.Scheduler.Interfaces.Services.Schedules;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Services.Schedules;

namespace Bacon.Scheduler.Tests.Services.Schedules;

[TestFixture]
public class SchedulerOccurrenceServiceWeeklyTests
{
    private ISchedulerOccurrenceService _service = null!;

    [SetUp]
    public void Setup() => _service = new SchedulerOccurrenceService();

    #region No weekdays specified (plain weekly interval)

    [Test]
    public void NoWeekdays_FreshSchedule_FutureStartDate_ReturnsStartDateUnchanged()
    {
        DateTime startDate = new(2026, 8, 5);
        DateTime currentDate = new(2026, 7, 30);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Weekly, startDate, null, null, 1, null, null, currentDate);

        Assert.That(result, Is.EqualTo(startDate));
    }

    [Test]
    public void NoWeekdays_FreshSchedule_PastStartDate_StepsForwardByEveryXWeeks()
    {
        DateTime startDate = new(2026, 6, 1); // Monday
        DateTime currentDate = new(2026, 7, 1);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Weekly, startDate, null, null, 2, null, null, currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 7, 13)));
    }

    [Test]
    public void NoWeekdays_EmptyWeekdaysCollection_BehavesSameAsNull()
    {
        DateTime startDate = new(2026, 6, 1);
        DateTime currentDate = new(2026, 7, 1);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Weekly, startDate, null, null, 2, [], null, currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 7, 13)));
    }

    #endregion

    #region Bugs / robustness checks

    [Test]
    public void NullWeekdays_AndEmptyWeekdays_ProduceIdenticalResults()
    {
        DateTime startDate = new(2026, 6, 1);
        DateTime currentDate = new(2026, 7, 1);

        DateTime? resultWithNull = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Weekly, startDate, null, null, 2, null, null, currentDate);
        DateTime? resultWithEmpty = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Weekly, startDate, null, null, 2, [], null, currentDate);

        Assert.That(resultWithNull, Is.EqualTo(resultWithEmpty));
    }

    // The only recursive path in the weekly calculation (rolling forward within the same week when
    // "today" is already a passed occurrence) removes the current weekday from a working copy of the
    // list on every call, so it is bounded by the list's length even with duplicate entries - unlike
    // Monthly's per-month recursion, it can never spin forever. This pins that bound down with a
    // pathological (duplicate-heavy) input.
    [Test]
    public void DuplicateWeekdayEntries_StillTerminatesWithCorrectResult()
    {
        DateTime startDate = new(2026, 8, 3, 7, 0, 0); // Monday
        DateTime currentDate = new(2026, 8, 3, 8, 0, 0); // same Monday, time already passed
        Weekdays[] weekdaysWithDuplicates = [Weekdays.Monday, Weekdays.Monday, Weekdays.Monday, Weekdays.Wednesday, Weekdays.Wednesday];

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Weekly, startDate, null, null, 1, weekdaysWithDuplicates, null, currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 8, 5, 7, 0, 0))); // Wednesday, same week
    }

    #endregion

    #region Single weekday

    [Test]
    public void SingleWeekday_StartDateAfterWeekdayInWeek_JumpsToNextWeekOccurrence()
    {
        DateTime startDate = new(2026, 8, 7); // Friday
        DateTime currentDate = new(2026, 1, 1);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Weekly, startDate, null, null, 1, [Weekdays.Monday], null, currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 8, 10))); // next Monday
    }

    [Test]
    public void SingleWeekday_TodayIsScheduledDayAndTimeNotYetPassed_ReturnsToday()
    {
        DateTime startDate = new(2026, 8, 3, 8, 0, 0); // Monday
        DateTime currentDate = new(2026, 8, 3, 7, 0, 0);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Weekly, startDate, null, null, 1, [Weekdays.Monday], null, currentDate);

        Assert.That(result, Is.EqualTo(startDate));
    }

    [Test]
    public void SingleWeekday_TodayIsScheduledDayButTimeAlreadyPassed_JumpsToNextWeek()
    {
        DateTime startDate = new(2026, 8, 3, 7, 0, 0); // Monday
        DateTime currentDate = new(2026, 8, 3, 8, 0, 0);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Weekly, startDate, null, null, 1, [Weekdays.Monday], null, currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 8, 10, 7, 0, 0)));
    }

    [Test]
    public void SingleWeekday_BiWeekly_SkipsIntermediateWeek()
    {
        DateTime startDate = new(2026, 1, 5); // Monday
        DateTime nextOccurrenceDate = new(2026, 7, 6); // Monday
        DateTime currentDate = new(2026, 7, 20); // Monday, 2 weeks later

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Weekly, startDate, null, nextOccurrenceDate, 2, [Weekdays.Monday], null, currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 7, 20)));
    }

    [Test]
    public void SingleWeekday_StartDateBeforeWeekdayInWeek_ReturnsSameWeekOccurrence()
    {
        DateTime startDate = new(2026, 8, 3); // Monday
        DateTime currentDate = new(2026, 1, 1);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Weekly, startDate, null, null, 1, [Weekdays.Friday], null, currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 8, 7))); // same week's Friday
    }

    #endregion

    #region Multiple weekdays sequence

    [Test]
    public void MultipleWeekdays_SequenceHitsEachSelectedDayInOrderThenRollsToNextWeek()
    {
        DateTime startDate = new(2026, 8, 3); // Monday
        DateTime currentDate = new(2026, 7, 1);
        Weekdays[] weekdays = [Weekdays.Monday, Weekdays.Wednesday, Weekdays.Friday];

        DateTime? first = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Weekly, startDate, null, null, 1, weekdays, null, currentDate);
        Assert.That(first, Is.EqualTo(new DateTime(2026, 8, 3)), "1st occurrence should be Monday");

        DateTime? second = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Weekly, startDate, null, first, 1, weekdays, null, first!.Value.AddTicks(1));
        Assert.That(second, Is.EqualTo(new DateTime(2026, 8, 5)), "2nd occurrence should be Wednesday");

        DateTime? third = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Weekly, startDate, null, second, 1, weekdays, null, second!.Value.AddTicks(1));
        Assert.That(third, Is.EqualTo(new DateTime(2026, 8, 7)), "3rd occurrence should be Friday");

        DateTime? fourth = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Weekly, startDate, null, third, 1, weekdays, null, third!.Value.AddTicks(1));
        Assert.That(fourth, Is.EqualTo(new DateTime(2026, 8, 10)), "4th occurrence should roll to next week's Monday");
    }

    #endregion

    #region End date

    [Test]
    public void EndDate_ExactlyEqualToComputedOccurrence_ReturnsNull()
    {
        DateTime startDate = new(2026, 8, 3);
        DateTime currentDate = new(2026, 8, 3);
        DateTime endDate = new(2026, 8, 3);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Weekly, startDate, endDate, null, 1, [Weekdays.Monday], null, currentDate);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void EndDate_AfterComputedOccurrence_ReturnsOccurrence()
    {
        DateTime startDate = new(2026, 8, 3);
        DateTime currentDate = new(2026, 8, 3);
        DateTime endDate = new(2026, 8, 4);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Weekly, startDate, endDate, null, 1, [Weekdays.Monday], null, currentDate);

        Assert.That(result, Is.EqualTo(startDate));
    }

    #endregion
}
