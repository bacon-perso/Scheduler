using Bacon.Scheduler.Interfaces.Services.Schedules;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Services.Schedules;

namespace Bacon.Scheduler.Tests.Services.Schedules;

[TestFixture]
public class SchedulerOccurrenceServiceMonthlyTests
{
    private ISchedulerOccurrenceService _service = null!;

    [SetUp]
    public void Setup() => _service = new SchedulerOccurrenceService();

    #region Single day of month

    [Test]
    public void SingleDay_FreshSchedule_FutureStartDate_ReturnsStartDateUnchanged()
    {
        DateTime startDate = new(2026, 8, 15);
        DateTime currentDate = new(2026, 7, 1);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Monthly, startDate, null, null, 1, null, ["15"], currentDate);

        Assert.That(result, Is.EqualTo(startDate));
    }

    [Test]
    public void SingleDay_PersistedNextOccurrenceAlreadyPassed_SkipsToNextValidMonth()
    {
        DateTime startDate = new(2026, 1, 1);
        DateTime nextOccurrenceDate = new(2026, 6, 15);
        DateTime currentDate = new(2026, 7, 20); // the 15th of July has already passed

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Monthly, startDate, null, nextOccurrenceDate, 1, null, ["15"], currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 8, 15)));
    }

    [Test]
    public void SingleDay_EveryTwoMonths_SkipsIntermediateMonth()
    {
        DateTime startDate = new(2026, 1, 15);
        DateTime nextOccurrenceDate = new(2026, 1, 15);
        DateTime currentDate = new(2026, 2, 1);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Monthly, startDate, null, nextOccurrenceDate, 2, null, ["15"], currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 3, 15)));
    }

    [Test]
    public void SingleDay_31_SkipsMonthsThatDontHaveIt()
    {
        DateTime startDate = new(2026, 1, 31);
        DateTime nextOccurrenceDate = new(2026, 3, 31);
        DateTime currentDate = new(2026, 4, 15); // April has only 30 days

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Monthly, startDate, null, nextOccurrenceDate, 1, null, ["31"], currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 5, 31)));
    }

    #endregion

    #region Multiple days of month sequence

    [Test]
    public void MultipleDays_SequenceHitsEachSelectedDayThenRollsToNextMonth()
    {
        DateTime startDate = new(2026, 8, 1);
        DateTime currentDate = new(2026, 7, 15);
        string[] daysOfMonth = ["1", "15", "L"];

        DateTime? first = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Monthly, startDate, null, null, 1, null, daysOfMonth, currentDate);
        Assert.That(first, Is.EqualTo(new DateTime(2026, 8, 1)), "1st occurrence should be Aug 1st");

        DateTime? second = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Monthly, startDate, null, first, 1, null, daysOfMonth, first!.Value.AddTicks(1));
        Assert.That(second, Is.EqualTo(new DateTime(2026, 8, 15)), "2nd occurrence should be Aug 15th");

        DateTime? third = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Monthly, startDate, null, second, 1, null, daysOfMonth, second!.Value.AddTicks(1));
        Assert.That(third, Is.EqualTo(new DateTime(2026, 8, 31)), "3rd occurrence should be Aug 31st (last day)");

        DateTime? fourth = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Monthly, startDate, null, third, 1, null, daysOfMonth, third!.Value.AddTicks(1));
        Assert.That(fourth, Is.EqualTo(new DateTime(2026, 9, 1)), "4th occurrence should roll to Sep 1st");
    }

    #endregion

    #region "L" (last day of month) with leap years

    [Test]
    public void LastDayOfMonth_ResolvesToFeb29InLeapYear()
    {
        DateTime startDate = new(2028, 1, 31); // 2028 is a leap year
        DateTime nextOccurrenceDate = new(2028, 1, 31);
        DateTime currentDate = new(2028, 2, 1);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Monthly, startDate, null, nextOccurrenceDate, 1, null, ["L"], currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2028, 2, 29)));
    }

    [Test]
    public void LastDayOfMonth_ResolvesToFeb28InNonLeapYear()
    {
        DateTime startDate = new(2026, 1, 31);
        DateTime nextOccurrenceDate = new(2026, 1, 31);
        DateTime currentDate = new(2026, 2, 1);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Monthly, startDate, null, nextOccurrenceDate, 1, null, ["L"], currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 2, 28)));
    }

    [Test]
    public void LastDayOfMonth_ResolvesToApril30ForA30DayMonth()
    {
        DateTime startDate = new(2026, 3, 31);
        DateTime nextOccurrenceDate = new(2026, 3, 31);
        DateTime currentDate = new(2026, 4, 1);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Monthly, startDate, null, nextOccurrenceDate, 1, null, ["L"], currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 4, 30)));
    }

    #endregion

    #region Empty days-of-month collection (falls back to startDate's own day)

    [Test]
    public void EmptyDaysOfMonth_FreshSchedule_FutureStartDate_ReturnsStartDateUnchanged()
    {
        DateTime startDate = new(2026, 8, 10);
        DateTime currentDate = new(2026, 7, 1);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Monthly, startDate, null, null, 1, null, [], currentDate);

        Assert.That(result, Is.EqualTo(startDate));
    }

    [Test]
    public void EmptyDaysOfMonth_PastStartDate_RecursOnStartDatesDayOfMonth()
    {
        DateTime startDate = new(2026, 1, 10);
        DateTime currentDate = new(2026, 3, 15);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Monthly, startDate, null, null, 1, null, [], currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 4, 10)));
    }

    #endregion

    #region Null days-of-month collection (regression test)

    // Regression test for a fixed bug: GetMonthlyNextOccurrenceDate used to special-case only a
    // non-null *empty* daysOfMonth collection as "fall back to startDate's own day of month" (via
    // `daysOfMonth?.Any() == false`), so a null collection fell through into the general branch with
    // an empty candidate-day list, which recursed one calendar month at a time with no base case ->
    // unbounded recursion -> StackOverflowException. The fix (`daysOfMonth == null || !daysOfMonth.Any()`)
    // now treats null the same as empty, so this should behave identically to the empty-collection case.
    [Test]
    public void NullDaysOfTheMonth_BehavesSameAsEmptyCollection()
    {
        DateTime startDate = new(2026, 1, 1);
        DateTime currentDate = new(2026, 7, 30);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Monthly, startDate, null, null, 1, null, null, currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 8, 1)));
    }

    #endregion

    #region End date

    [Test]
    public void EndDate_ExactlyEqualToComputedOccurrence_ReturnsNull()
    {
        DateTime startDate = new(2026, 8, 15);
        DateTime currentDate = new(2026, 8, 15);
        DateTime endDate = new(2026, 8, 15);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Monthly, startDate, endDate, null, 1, null, ["15"], currentDate);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void EndDate_AfterComputedOccurrence_ReturnsOccurrence()
    {
        DateTime startDate = new(2026, 8, 15);
        DateTime currentDate = new(2026, 8, 15);
        DateTime endDate = new(2026, 8, 16);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Monthly, startDate, endDate, null, 1, null, ["15"], currentDate);

        Assert.That(result, Is.EqualTo(startDate));
    }

    #endregion
}
