namespace Bacon.Scheduler.Models.Schedules;

/// <summary>
/// Defines a schedule config
/// </summary>
public sealed class ScheduleRecurrenceConfigs
{
    /// <summary>
    /// The schedule's recurrence type
    /// </summary>
    public required ScheduleRecurrenceTypes ScheduleRecurrenceType { get; set; }

    /// <summary>
    /// The schedule's initial start date
    /// </summary>
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// The schedule's last run date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Defines if the config runs every x days/weeks/months based on the recurrence type
    /// Accepted values : 
    /// ScheduleRecurrenceType.hourly => [1, 24]
    /// ScheduleRecurrenceType.daily => [1, 365]
    /// ScheduleRecurrenceType.weekly => [1, 52]
    /// ScheduleRecurrenceType.monthly => [1, 12]
    /// 
    /// </summary>
    public required short EveryX { get; set; }

    /// <summary>
    /// Defines the day of the week the weekly schedule will run on
    /// </summary>
    public IEnumerable<Weekdays>? Weekdays { get; set; }

    /// <summary>
    /// Defines the days of the month that the monthly schedule will run on (0-31 + L for last day of the month)
    /// </summary>
    public IEnumerable<string>? DaysOfTheMonth { get; set; }
}