using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Schedules;

namespace Bacon.Scheduler.Interfaces.Services.Schedules;

internal interface IScheduleValidationService
{
    void ValidateSystemSchedules(IEnumerable<Schedule> schedules);

    void ValidateScheduleRetry(string id, byte retryLimit);

    void ValidateUtcDates(DateTime? startDate, DateTime? endDate);

    void ValidateStartAndEndDate(DateTime startDate, DateTime? endDate, DateTime currentDate);

    void ValidateScheduleRecurrence(string id, ScheduleRecurrenceTypes scheduleRecurrenceType, short everyX, IEnumerable<Weekdays>? weekdays, IEnumerable<string>? dayOfTheMonths);

    void ValidateMonthlyDayOfTheMonths(string id, ScheduleRecurrenceTypes scheduleRecurrenceType, IEnumerable<string>? dayOfTheMonths);

    void ValidateScheduleFrequency(ScheduleRecurrenceTypes scheduleRecurrenceType, short everyX);

    void ValidateImpossibleRecurrence(string id, ScheduleRecurrenceTypes scheduleRecurrenceType, DateTime startDate, short everyX, IEnumerable<string>? dayOfTheMonths);

    Task ValidateScheduleNameExist(Guid? scheduleId, string scheduleName, CancellationToken cancellationToken);
}