using Bacon.Scheduler.Models;

namespace Bacon.Scheduler.Interfaces.Services.Schedules;

internal interface ISchedulerOccurrenceService
{
    DateTime? GetNextOccurrenceDate(ScheduleRecurrenceTypes recurrenceType, DateTime startDate, DateTime? endDate, DateTime? nextOccurrenceDate, short everyX, IEnumerable<Weekdays>? weekdays, IEnumerable<string>? dayOfTheMonths, DateTime currentDate);
}