using Bacon.Scheduler.Interfaces.Services.Schedules;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Services.Schedules;

namespace Bacon.Scheduler.Tests.Services.Schedules;

[TestFixture]
public class SchedulerOccurrenceServiceDailyTests
{
    private ISchedulerOccurrenceService _service = null!;

    [SetUp]
    public void Setup() => _service = new SchedulerOccurrenceService();

    [Test]
    public void FreshSchedule_FutureStartDate_ReturnsStartDateUnchanged()
    {
        DateTime startDate = new(2026, 8, 15);
        DateTime currentDate = new(2026, 7, 30);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Daily, startDate, null, null, 1, null, null, currentDate);

        Assert.That(result, Is.EqualTo(startDate));
    }

    [Test]
    public void FreshSchedule_PastStartDate_CatchesUpExactlyToCurrentDate()
    {
        DateTime startDate = new(2026, 7, 1);
        DateTime currentDate = new(2026, 7, 30);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Daily, startDate, null, null, 29, null, null, currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 7, 30)));
    }

    [Test]
    public void PersistedNextOccurrence_CrossesMonthBoundary()
    {
        DateTime startDate = new(2026, 1, 1);
        DateTime nextOccurrenceDate = new(2026, 1, 31);
        DateTime currentDate = new(2026, 2, 1);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Daily, startDate, null, nextOccurrenceDate, 1, null, null, currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 2, 1)));
    }

    [Test]
    public void PersistedNextOccurrence_CrossesFeb29InLeapYear()
    {
        DateTime startDate = new(2024, 1, 1);
        DateTime nextOccurrenceDate = new(2024, 2, 28);
        DateTime currentDate = new(2024, 2, 29);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Daily, startDate, null, nextOccurrenceDate, 1, null, null, currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2024, 2, 29)));
    }

    [Test]
    public void PersistedNextOccurrence_SkipsFeb29InNonLeapYear()
    {
        DateTime startDate = new(2026, 1, 1);
        DateTime nextOccurrenceDate = new(2026, 2, 28);
        DateTime currentDate = new(2026, 3, 1);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Daily, startDate, null, nextOccurrenceDate, 1, null, null, currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 3, 1)));
    }

    [Test]
    public void PersistedNextOccurrence_CrossesYearBoundary()
    {
        DateTime startDate = new(2025, 1, 1);
        DateTime nextOccurrenceDate = new(2025, 12, 31);
        DateTime currentDate = new(2026, 1, 1);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Daily, startDate, null, nextOccurrenceDate, 1, null, null, currentDate);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 1, 1)));
    }

    [Test]
    public void EndDate_ExactlyEqualToComputedOccurrence_ReturnsNull()
    {
        DateTime startDate = new(2026, 7, 30);
        DateTime currentDate = new(2026, 7, 30);
        DateTime endDate = new(2026, 7, 30);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Daily, startDate, endDate, null, 1, null, null, currentDate);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void EndDate_AfterComputedOccurrence_ReturnsOccurrence()
    {
        DateTime startDate = new(2026, 7, 30);
        DateTime currentDate = new(2026, 7, 30);
        DateTime endDate = new(2026, 7, 31);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Daily, startDate, endDate, null, 1, null, null, currentDate);

        Assert.That(result, Is.EqualTo(startDate));
    }

    [TestCase((short)1)]
    [TestCase((short)7)]
    [TestCase((short)30)]
    [TestCase((short)365)]
    public void PersistedNextOccurrence_StepsForwardByEveryXDaysForVariousIntervals(short everyX)
    {
        DateTime startDate = new(2020, 1, 1);
        DateTime nextOccurrenceDate = new(2026, 1, 1);
        DateTime currentDate = nextOccurrenceDate.AddDays(everyX * 2);

        DateTime? result = _service.GetNextOccurrenceDate(ScheduleRecurrenceTypes.Daily, startDate, null, nextOccurrenceDate, everyX, null, null, currentDate);

        Assert.That(result, Is.EqualTo(currentDate));
    }
}
