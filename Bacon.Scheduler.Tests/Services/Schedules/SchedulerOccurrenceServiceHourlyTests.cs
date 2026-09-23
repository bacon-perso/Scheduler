using Bacon.Scheduler.Interfaces.Services.Schedules;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Services.Schedules;

namespace Bacon.Scheduler.Tests.Services.Schedules;

[TestFixture]
public class SchedulerOccurrenceServiceHourlyTests
{
    private ISchedulerOccurrenceService _service = null!;

    [SetUp]
    public void Setup() => _service = new SchedulerOccurrenceService();

    [Test]
    public void FreshSchedule_FutureStartDate_ReturnsStartDateUnchanged()
    {
        DateTime startDate = new(2026, 8, 1, 10, 0, 0);
        DateTime currentDate = new(2026, 7, 30, 8, 0, 0);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Hourly, startDate, null, null, 1, null, null, currentDate);

        Assert.That(result, Is.EqualTo(startDate));
    }

    [Test]
    public void FreshSchedule_PastStartDate_CatchesUpExactlyToCurrentDate()
    {
        DateTime startDate = new(2026, 7, 29, 8, 0, 0);
        DateTime currentDate = new(2026, 7, 30, 8, 0, 0);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Hourly, startDate, null, null, 24, null, null, currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 7, 30, 8, 0, 0)));
    }

    [Test]
    public void PersistedNextOccurrenceInPast_AdvancesByEveryXHoursUntilCurrentDate()
    {
        DateTime startDate = new(2026, 1, 1, 0, 0, 0);
        DateTime nextOccurrenceDate = new(2026, 7, 29, 8, 0, 0);
        DateTime currentDate = new(2026, 7, 30, 8, 0, 0);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Hourly, startDate, null, nextOccurrenceDate, 6, null, null, currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 7, 30, 8, 0, 0)));
    }

    [Test]
    public void EndDate_ExactlyEqualToComputedOccurrence_ReturnsNull()
    {
        DateTime startDate = new(2026, 7, 30, 8, 0, 0);
        DateTime currentDate = new(2026, 7, 30, 8, 0, 0);
        DateTime endDate = new(2026, 7, 30, 8, 0, 0);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Hourly, startDate, endDate, null, 1, null, null, currentDate);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void EndDate_BeforeComputedOccurrence_ReturnsNull()
    {
        DateTime startDate = new(2026, 7, 30, 6, 0, 0);
        DateTime currentDate = new(2026, 7, 30, 8, 0, 0);
        DateTime endDate = new(2026, 7, 30, 7, 0, 0);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Hourly, startDate, endDate, null, 1, null, null, currentDate);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void EndDate_AfterComputedOccurrence_ReturnsOccurrence()
    {
        DateTime startDate = new(2026, 7, 30, 8, 0, 0);
        DateTime currentDate = new(2026, 7, 30, 8, 0, 0);
        DateTime endDate = new(2026, 7, 30, 9, 0, 0);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Hourly, startDate, endDate, null, 1, null, null, currentDate);

        Assert.That(result, Is.EqualTo(startDate));
    }

    [TestCase((short)1)]
    [TestCase((short)2)]
    [TestCase((short)6)]
    [TestCase((short)12)]
    [TestCase((short)24)]
    public void PersistedNextOccurrence_StepsForwardByEveryXHoursForVariousIntervals(short everyX)
    {
        DateTime startDate = new(2020, 1, 1, 0, 0, 0);
        DateTime nextOccurrenceDate = new(2026, 7, 1, 0, 0, 0);
        DateTime currentDate = nextOccurrenceDate.AddHours(everyX * 3);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Hourly, startDate, null, nextOccurrenceDate, everyX, null, null, currentDate);

        Assert.That(result, Is.EqualTo(currentDate));
    }
}
