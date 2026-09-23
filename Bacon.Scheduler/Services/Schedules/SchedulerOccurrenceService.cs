using Bacon.Scheduler.Interfaces.Services.Schedules;
using Bacon.Scheduler.Models;
using System.Globalization;

namespace Bacon.Scheduler.Services.Schedules;

internal sealed class SchedulerOccurrenceService : ISchedulerOccurrenceService
{
    #region Get

    public DateTime? GetNextOccurrenceDate(ScheduleRecurrenceTypes recurrenceType, DateTime startDate, DateTime? endDate, DateTime? nextOccurrenceDate, short everyX, IEnumerable<Weekdays>? weekdays, IEnumerable<string>? daysOfMonth, DateTime currentDate)
    {
        return GetNextOccurrence(recurrenceType, startDate, endDate, nextOccurrenceDate, everyX, weekdays, daysOfMonth, currentDate);
    }

    #endregion Get

    #region Private

    private static DateTime? GetNextOccurrence(ScheduleRecurrenceTypes scheduleRecurrenceTypes, DateTime startDate, DateTime? endDate, DateTime? nextRunDate, short everyX, IEnumerable<Weekdays>? weekdays, IEnumerable<string>? daysOfMonth, DateTime currentDate)
    {
        List<byte> lstDaysOfTheMonths = [];
        if (scheduleRecurrenceTypes == ScheduleRecurrenceTypes.Monthly)
        {
            if (nextRunDate != null && nextRunDate >= startDate)
            {
                lstDaysOfTheMonths = ConvertDaysOfTheMonthToListBytes(nextRunDate.Value, daysOfMonth);
            }
            else
            {
                lstDaysOfTheMonths = ConvertDaysOfTheMonthToListBytes(startDate, daysOfMonth);
            }
        }

        List<byte> _weekdays = weekdays?.Select(s => (byte)s).ToList() ?? [];

        while (true)
        {
            nextRunDate = scheduleRecurrenceTypes switch
            {
                ScheduleRecurrenceTypes.Hourly => GetHourlyNextOccurrenceDate(startDate, nextRunDate, everyX, currentDate),
                ScheduleRecurrenceTypes.Daily => GetDailyNextOccurrenceDate(startDate, nextRunDate, everyX, currentDate),
                ScheduleRecurrenceTypes.Weekly => GetWeeklyNextOccurrenceDate(startDate, nextRunDate, everyX, weekdays, currentDate, _weekdays),
                ScheduleRecurrenceTypes.Monthly => GetMonthlyNextOccurrenceDate(startDate, nextRunDate, everyX, daysOfMonth, currentDate, lstDaysOfTheMonths),
                _ => startDate,
            };

            if (nextRunDate == null)
            {
                return null;
            }

            if (nextRunDate.Value >= currentDate)
            {
                if (endDate != null && nextRunDate.Value >= endDate.Value)
                {
                    return null;
                }

                return nextRunDate.Value;
            }
        }
    }

    #region Hourly

    private static DateTime GetHourlyNextOccurrenceDate(DateTime startDate, DateTime? nextRunDate, short everyX, DateTime currentDate)
    {
        return (nextRunDate == null || startDate >= currentDate) ? startDate : nextRunDate.Value.AddHours(everyX);
    }

    #endregion Hourly

    #region Daily

    private static DateTime GetDailyNextOccurrenceDate(DateTime startDate, DateTime? nextRunDate, short everyX, DateTime currentDate)
    {
        return (nextRunDate == null || startDate >= currentDate) ? startDate : nextRunDate.Value.AddDays(everyX);
    }

    #endregion Daily

    #region Weekly

    private static DateTime GetWeeklyNextOccurrenceDate(DateTime startDate, DateTime? nextRunDate, short everyX, IEnumerable<Weekdays>? weekdays, DateTime currentDate, List<byte> workingWeekdays)
    {
        if (nextRunDate != null && nextRunDate >= startDate)
        {
            startDate = nextRunDate.Value;
        }

        if (weekdays == null || !weekdays.Any())
        {
            return (nextRunDate == null && startDate >= currentDate) ? startDate : nextRunDate == null ? AddWeeks(startDate, everyX) : AddWeeks(nextRunDate.Value, everyX);
        }

        byte currentDayOfTheWeek = (byte)startDate.DayOfWeek;

        workingWeekdays = [.. workingWeekdays.Where(w => w >= currentDayOfTheWeek)];

        // if the next working week day is not in current week
        if (workingWeekdays.Count == 0)
        {
            startDate = new GregorianCalendar().AddDays(startDate, -(int)startDate.DayOfWeek + weekdays.Select(s => (byte)s).OrderBy(o => o).First());

            return AddWeeks(startDate, everyX);
        }

        // if current day is in the current working week days.
        if (workingWeekdays.Contains(currentDayOfTheWeek))
        {
            if (startDate < currentDate)
            {
                workingWeekdays.Remove(currentDayOfTheWeek);

                return GetWeeklyNextOccurrenceDate(startDate, nextRunDate, everyX, weekdays, currentDate, workingWeekdays);
            }

            return startDate;
        }

        int daysToAddEveryX = (workingWeekdays.OrderBy(o => o).First() - (byte)startDate.DayOfWeek + 7) % 7;

        return startDate.AddDays(daysToAddEveryX);
    }

    private static DateTime AddWeeks(DateTime dateTime, short everyX) => dateTime.AddDays(7 * everyX);

    #endregion Weekly

    #region Monthly

    private static DateTime GetMonthlyNextOccurrenceDate(DateTime startDate, DateTime? nextRunDate, short everyX, IEnumerable<string>? daysOfMonth, DateTime currentDate, List<byte> workingDaysOfTheMonths)
    {
        if (nextRunDate != null && nextRunDate >= startDate)
        {
            startDate = nextRunDate.Value;
        }

        if (daysOfMonth == null || !daysOfMonth.Any())
        {
            return (nextRunDate == null && startDate >= currentDate) ? startDate : GetNextAvailableDayOfTheMonth(startDate, everyX, [startDate.Day.ToString(CultureInfo.InvariantCulture)], currentDate);
        }

        byte currentDayOfTheMonth = (byte)startDate.Day;

        workingDaysOfTheMonths = [.. workingDaysOfTheMonths.Where(w => w >= currentDayOfTheMonth).OrderBy(o => o)];

        IEnumerable<byte> listOfDays = ConvertDaysOfTheMonthToListBytes(startDate, daysOfMonth);

        if (workingDaysOfTheMonths.Count == 0)
        {
            return GetNextAvailableDayOfTheMonth(startDate, everyX, daysOfMonth, currentDate);
        }

        if (workingDaysOfTheMonths.Contains(currentDayOfTheMonth))
        {
            if (startDate < currentDate)
            {
                workingDaysOfTheMonths.Remove(currentDayOfTheMonth);

                return GetMonthlyNextOccurrenceDate(startDate, nextRunDate, everyX, daysOfMonth, currentDate, workingDaysOfTheMonths);
            }

            return startDate;
        }

        byte workingDay = workingDaysOfTheMonths.First();

        if (!IsDateValid(startDate, workingDay))
        {
            startDate = AddMonth(GetSpecificDayOfTheMonth(startDate, 1), everyX, daysOfMonth);

            return GetMonthlyNextOccurrenceDate(startDate, nextRunDate, everyX, daysOfMonth, currentDate, [.. listOfDays]);
        }

        return GetSpecificDayOfTheMonth(startDate, workingDay);
    }

    private static List<byte> ConvertDaysOfTheMonthToListBytes(DateTime startDate, IEnumerable<string>? daysOfMonth)
    {
        if (daysOfMonth == null)
        {
            return [];
        }

        return [.. daysOfMonth.Select(s =>
        {
            if (string.Equals(s, "L", StringComparison.OrdinalIgnoreCase))
            {
                return (byte)CultureInfo.InvariantCulture.Calendar.GetDaysInMonth(startDate.Year, startDate.Month);
            }

            return byte.Parse(s, CultureInfo.InvariantCulture);
        }).Distinct().OrderBy(o => o)];
    }

    private static DateTime GetNextAvailableDayOfTheMonth(DateTime startDate, short everyX, IEnumerable<string>? daysOfMonth, DateTime currentDate)
    {
        IEnumerable<byte> _dayOfTheMonths = ConvertDaysOfTheMonthToListBytes(startDate, daysOfMonth);

        foreach (byte dayOfTheMonth in _dayOfTheMonths)
        {
            if (IsDateValid(startDate, dayOfTheMonth))
            {
                startDate = GetSpecificDayOfTheMonth(startDate, dayOfTheMonth);

                if (startDate > currentDate)
                {
                    return startDate;
                }
            }
        }

        startDate = GetSpecificDayOfTheMonth(startDate, 1);

        startDate = AddMonth(startDate, everyX, daysOfMonth);

        return GetNextAvailableDayOfTheMonth(startDate, everyX, daysOfMonth, currentDate);
    }

    private static DateTime AddMonth(DateTime dateTime, short everyX, IEnumerable<string>? daysOfMonth)
    {
        DateTime myDate = GetSpecificDayOfTheMonth(dateTime, 1).AddMonths(everyX);

        IEnumerable<byte> _daysOfTheMonth = ConvertDaysOfTheMonthToListBytes(myDate, daysOfMonth);

        foreach (byte day in _daysOfTheMonth)
        {
            if (IsDateValid(myDate, day))
            {
                return GetSpecificDayOfTheMonth(myDate, day);
            }
        }

        return AddMonth(myDate, everyX, daysOfMonth);
    }

    private static DateTime GetSpecificDayOfTheMonth(DateTime date, byte dayOfTheMonth) => new(date.Year, date.Month, dayOfTheMonth, date.Hour, date.Minute, date.Second);

    // Check if the date is valid in the month of the date passed in parameter
    private static bool IsDateValid(DateTime dateTime, byte nextDayOfTheMonth) => nextDayOfTheMonth <= DateTime.DaysInMonth(dateTime.Year, dateTime.Month);

    #endregion Monthly

    #endregion Private
}