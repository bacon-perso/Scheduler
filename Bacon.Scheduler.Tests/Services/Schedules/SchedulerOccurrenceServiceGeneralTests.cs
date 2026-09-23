using Bacon.Scheduler.Interfaces.Services.Schedules;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Services.Schedules;

namespace Bacon.Scheduler.Tests.Services.Schedules;

[TestFixture]
public class SchedulerOccurrenceServiceGeneralTests
{
    private ISchedulerOccurrenceService _service = null!;

    [SetUp]
    public void Setup() => _service = new SchedulerOccurrenceService();

    [Test]
    public void UnmappedRecurrenceType_FutureStartDate_ReturnsStartDateUnchanged()
    {
        // Simulates a recurrence type value outside the defined enum range hitting the switch's default branch.
        DateTime startDate = new(2026, 8, 1);
        DateTime currentDate = new(2026, 7, 1);

        DateTime? result = _service.GetNextOccurrenceDate((ScheduleRecurrenceTypes)99, startDate, null, null, 1, null, null, currentDate);

        Assert.That(result, Is.EqualTo(startDate));
    }

    [Test]
    public void Hourly_IgnoresWeekdaysAndDaysOfMonthParameters()
    {
        DateTime startDate = new(2026, 8, 1, 10, 0, 0);
        DateTime currentDate = new(2026, 7, 30, 8, 0, 0);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Hourly, startDate, null, null, 1, [Weekdays.Monday, Weekdays.Friday], ["1", "15", "L"], currentDate);

        Assert.That(result, Is.EqualTo(startDate));
    }

    [Test]
    public void Daily_IgnoresWeekdaysAndDaysOfMonthParameters()
    {
        DateTime startDate = new(2026, 8, 1);
        DateTime currentDate = new(2026, 7, 30);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Daily, startDate, null, null, 1, [Weekdays.Monday, Weekdays.Friday], ["1", "15", "L"], currentDate);

        Assert.That(result, Is.EqualTo(startDate));
    }

    [TestCase(ScheduleRecurrenceTypes.Hourly)]
    [TestCase(ScheduleRecurrenceTypes.Daily)]
    [TestCase(ScheduleRecurrenceTypes.Weekly)]
    [TestCase(ScheduleRecurrenceTypes.Monthly)]
    public void CurrentDateExactlyOnStartDate_ReturnsStartDate(ScheduleRecurrenceTypes recurrenceType)
    {
        DateTime startDate = new(2026, 8, 3); // a Monday, also usable as a day-of-month value
        DateTime currentDate = startDate;

        IEnumerable<string>? daysOfMonth = recurrenceType == ScheduleRecurrenceTypes.Monthly ? ["3"] : null;
        IEnumerable<Weekdays>? weekdays = recurrenceType == ScheduleRecurrenceTypes.Weekly ? [Weekdays.Monday] : null;

        DateTime? result = _service.GetNextOccurrenceDate(recurrenceType, startDate, null, null, 1, weekdays, daysOfMonth, currentDate);

        Assert.That(result, Is.EqualTo(startDate));
    }
}
