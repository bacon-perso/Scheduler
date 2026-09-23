using System.Linq.Expressions;

namespace Bacon.Scheduler.Models.Schedules;

internal sealed record SystemSchedule(string ScheduleName, bool IsActive, ScheduleRecurrenceTypes ScheduleRecurrenceType, TimeOnly ScheduleTime, short EveryX, IReadOnlyCollection<Weekdays>? Weekdays, IReadOnlyCollection<string>? DayOfTheMonths
    , bool AutoRestartOnFailure, byte RetryLimit, LambdaExpression MethodCall, Type JobType, Type JobLogStoreImplementationType);