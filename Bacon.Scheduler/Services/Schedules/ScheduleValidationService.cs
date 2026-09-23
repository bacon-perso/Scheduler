using Bacon.Scheduler.Interfaces.Services.Schedules;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Schedules;
using System.Data;

namespace Bacon.Scheduler.Services.Schedules;

internal sealed class ScheduleValidationService(IScheduleStore scheduleStore) : IScheduleValidationService
{
    public void ValidateSystemSchedules(IEnumerable<Schedule> schedules)
    {
        IEnumerable<Guid> invalidSchedules = schedules.Where(w => w.IsSystem).Select(s => s.ScheduleId);

        if (invalidSchedules.Any())
        {
            throw new InvalidOperationException($"Scheduler - The following system schedules cannot be modified: {string.Join(", ", invalidSchedules)}");
        }
    }

    public void ValidateScheduleRetry(string id, byte retryLimit)
    {
        if (retryLimit < 0 || retryLimit > 10)
        {
            throw new ArgumentException($"Scheduler - There was an error with the schedule '{id}', the retry limit can only support a value between 0 and 10", nameof(retryLimit));
        }
    }

    public void ValidateUtcDates(DateTime? startDate, DateTime? endDate)
    {
        if (startDate != null && !startDate.Value.Kind.Equals(DateTimeKind.Utc))
        {
            throw new ArgumentException("Scheduler - The start date requires to be in UTC", nameof(startDate));
        }

        if (endDate != null && !endDate.Value.Kind.Equals(DateTimeKind.Utc))
        {
            throw new ArgumentException("Scheduler - The end date requires to be in UTC", nameof(startDate));
        }
    }

    public void ValidateStartAndEndDate(DateTime startDate, DateTime? endDate, DateTime currentDate)
    {
        if (startDate < currentDate)
        {
            throw new ArgumentOutOfRangeException(nameof(startDate), "Scheduler - The start date cannot be in the past");
        }

        if (endDate != null && startDate > endDate.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(endDate), "Scheduler - The start date cannot greater than the end date");
        }
    }

    public void ValidateScheduleRecurrence(string id, ScheduleRecurrenceTypes scheduleRecurrenceType, short everyX, IEnumerable<Weekdays>? weekdays, IEnumerable<string>? dayOfTheMonths)
    {
        ValidateScheduleFrequency(scheduleRecurrenceType, everyX);

        if (scheduleRecurrenceType != ScheduleRecurrenceTypes.Weekly && weekdays?.Any() == true)
        {
            throw new ArgumentException($"Scheduler - There was an error with the schedule '{id}', the schedule recurrence does not support weekdays", nameof(weekdays));
        }

        if (scheduleRecurrenceType !=ScheduleRecurrenceTypes.Monthly && dayOfTheMonths?.Any() == true)
        {
            throw new ArgumentException($"Scheduler - There was an error with the schedule '{id}', the schedule recurrence does not support days of the month", nameof(dayOfTheMonths));
        }
    }

    public void ValidateMonthlyDayOfTheMonths(string id, ScheduleRecurrenceTypes scheduleRecurrenceType, IEnumerable<string>? dayOfTheMonths)
    {
        if (scheduleRecurrenceType != ScheduleRecurrenceTypes.Monthly || dayOfTheMonths == null)
        {
            return;
        }

        List<string?> invalidDayValuesInRange = [];
        foreach (string? day in dayOfTheMonths)
        {
            if (string.IsNullOrEmpty(day))
            {
                invalidDayValuesInRange.Add(day);
                continue;
            }

            bool dayHasLetter = day.Any(char.IsLetter);

            if (dayHasLetter && !day.Equals("L", StringComparison.OrdinalIgnoreCase))
            {
                invalidDayValuesInRange.Add(day);
                continue;
            }

            if (dayHasLetter)
            {
                continue;
            }

            if (!byte.TryParse(day, out byte currentDayOfTheMonth))
            {
                invalidDayValuesInRange.Add(day);
                continue;
            }

            if (currentDayOfTheMonth < 1 || currentDayOfTheMonth > 31)
            {
                invalidDayValuesInRange.Add(day);
            }
        }

        if (invalidDayValuesInRange.Count > 0)
        {
            throw new ArgumentException($"Scheduler - There was an error with the schedule '{id}', the schedule contains invalid day(s) of the month: {string.Join(", ", invalidDayValuesInRange.Select(s => s ?? "null"))}");
        }
    }

    public void ValidateScheduleFrequency(ScheduleRecurrenceTypes scheduleRecurrenceType, short everyX)
    {
        (short minValue, short maxValue) = scheduleRecurrenceType switch
        {
            ScheduleRecurrenceTypes.Hourly => ((short)1, (short)24),
            ScheduleRecurrenceTypes.Daily => ((short)1, (short)365),
            ScheduleRecurrenceTypes.Weekly => ((short)1, (short)52),
            ScheduleRecurrenceTypes.Monthly => ((short)1, (short)12),
            _ => throw new InvalidOperationException("Invalid recurrence type")
        };

        if (everyX < minValue || everyX > maxValue)
        {
            throw new ArgumentException($"Scheduler - The recurrence type '{scheduleRecurrenceType}' only accepts a value between {minValue} and {maxValue} for the property 'EveryX'' ");
        }
    }

    public void ValidateImpossibleRecurrence(string id, ScheduleRecurrenceTypes scheduleRecurrenceType, DateTime startDate, short everyX, IEnumerable<string>? dayOfTheMonths)
    {
        if (scheduleRecurrenceType != ScheduleRecurrenceTypes.Monthly || (scheduleRecurrenceType == ScheduleRecurrenceTypes.Monthly && !everyX.Equals(12)))
        {
            return;
        }

        IEnumerable<string> _daysOfTheMonth = dayOfTheMonths?.Select(s => s!) ?? [];

        List<string> remainingDaysOfMonth = [.. _daysOfTheMonth];

        //handle february
        if (startDate.Month.Equals(2))
        {
            List<string> invalidDays = ["30", "31"];

            remainingDaysOfMonth.RemoveAll((s) => invalidDays.Contains(s.Trim(), StringComparer.OrdinalIgnoreCase));

            //if there are no other days than 30-31, then it means the schedule for february is invalid
            if (remainingDaysOfMonth.Count == 0)
            {
                throw new ArgumentException($"Scheduler - There was an error with the schedule '{id}', the following days of the months are invalid for the month '{startDate.Month}', when 'EveryX' is 12 : {string.Join(", ", invalidDays)}");
            }

            return;
        }

        int lastDayInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);

        remainingDaysOfMonth.Remove("31");

        //if the month contains 30 days and the remainingDaysOfMonth only contained 31
        if (lastDayInMonth.Equals(30) && remainingDaysOfMonth.Count == 0 && _daysOfTheMonth.Any())
        {
            throw new ArgumentException($"Scheduler - There was an error with the schedule '{id}', the following days of the months are invalid for the month '{startDate.Month}', when 'EveryX' is 12 : 31");
        }
    }

    public async Task ValidateScheduleNameExist(Guid? scheduleId, string scheduleName, CancellationToken cancellationToken)
    {
        bool scheduleNameExists = await scheduleStore.CheckScheduleNameExistsAsync(scheduleId, scheduleName, cancellationToken).ConfigureAwait(false);
        if (scheduleNameExists)
        {
            throw new DuplicateNameException(scheduleName);
        }
    }
}