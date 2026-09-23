using Bacon.Scheduler.Extensions;
using Bacon.Scheduler.Interfaces.Services.Schedules;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Models.Schedules;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Bacon.Scheduler.Services.Schedules;

internal sealed partial class InternalScheduleService(IScheduleStore scheduleStore, ISchedulerOccurrenceService schedulerOccurrenceService, ILogger<InternalScheduleService> logger) : IInternalScheduleService
{
    #region Get

    public async Task<IEnumerable<SchedulerWrapper>> GetScheduleToRunAsync(DateTime currentDate, CancellationToken cancellationToken)
    {
        return [.. await scheduleStore.GetSchedulesToRunAsync(currentDate, cancellationToken)];
    }

    public async Task<IEnumerable<Schedule>> GetAllSchedulesAsync(CancellationToken cancellationToken)
    {
        return [.. await scheduleStore.GetAllSchedulesAsync(cancellationToken)];
    }

    #endregion Get

    #region Insert

    public async Task<Schedule> InsertScheduleAsync(string scheduleName, bool isSystem, ScheduleRecurrenceTypes scheduleRecurrenceType, DateTime startDate, DateTime? endDate, short everyX, IEnumerable<Weekdays>? weekdays, IEnumerable<string>? dayOfTheMonths
        , bool autoRestartOnFailure, byte retryLimit, [NotNull] LambdaExpression methodCall, Type? explicitMethodType, Type jobLogStoreImplementationType, DateTime currentDate, CancellationToken cancellationToken)
    {
        DateTime? nextOccurrenceDate = schedulerOccurrenceService.GetNextOccurrenceDate(
            scheduleRecurrenceType,
            startDate,
            endDate,
            null,
            everyX,
            weekdays,
            dayOfTheMonths,
            currentDate);

        Schedule schedule = new()
        {
            ScheduleId = Guid.NewGuid(),
            ScheduleName = scheduleName,
            IsSystem = isSystem,
            IsActive = true,
            CreationDate = currentDate,
            ModificationDate = null,
            ScheduleRecurrenceConfig = new()
            {
                ScheduleRecurrenceType = scheduleRecurrenceType,
                EveryX = everyX,
                StartDate = startDate,
                EndDate = endDate,
                Weekdays = weekdays,
                DaysOfTheMonth = dayOfTheMonths
            },
            ScheduleExecutionStatus = ScheduleExecutionStatuses.Ready,
            LastOccurrenceDate = null,
            LastOccurrenceStatus = ScheduleOccurrenceStatuses.Never,
            NextOccurrenceDate = nextOccurrenceDate,
            ScheduleRetryConfig = new()
            {
                AutoRestartOnFailure = autoRestartOnFailure,
                RetryLimit = retryLimit,
            }
        };

        SchedulerWrapper schedulerWrapper = new()
        {
            Schedule = schedule,
            JobExecutionMetadata = GetJobExecutionMetadata(methodCall, explicitMethodType, jobLogStoreImplementationType)
        };

        SchedulerWrapper? returnedScheduleWrapper = await scheduleStore.InsertScheduleAsync(schedulerWrapper, cancellationToken).ConfigureAwait(false);

        return returnedScheduleWrapper?.Schedule ?? throw new KeyNotFoundException($"Scheduler - The schedule '{schedule.ScheduleId}' cannot be found");
    }

    #endregion Insert

    #region Update

    public async Task<Schedule> UpdateScheduleNameAsync(Guid scheduleId, string scheduleName, DateTime currentDate, CancellationToken cancellationToken)
    {
        Schedule? updatedSchedule = await scheduleStore.UpdateScheduleNameAsync(scheduleId, scheduleName, currentDate, cancellationToken).ConfigureAwait(false);

        return updatedSchedule ?? throw new KeyNotFoundException($"Scheduler - The schedule '{scheduleId}' cannot be found after update");
    }

    public async Task<Schedule> UpdateScheduleStatusAsync(Schedule schedule, bool isActive, DateTime currentDate, CancellationToken cancellationToken)
    {
        DateTime? nextScheduleOccurrenceDate = schedule.NextOccurrenceDate;
        if (!schedule.IsActive.Equals(isActive) && isActive)
        {
            nextScheduleOccurrenceDate = schedulerOccurrenceService.GetNextOccurrenceDate(
                schedule.ScheduleRecurrenceConfig.ScheduleRecurrenceType,
                schedule.ScheduleRecurrenceConfig.StartDate,
                schedule.ScheduleRecurrenceConfig.EndDate,
                schedule.NextOccurrenceDate,
                schedule.ScheduleRecurrenceConfig.EveryX,
                schedule.ScheduleRecurrenceConfig.Weekdays,
                schedule.ScheduleRecurrenceConfig.DaysOfTheMonth,
                currentDate);
        }

        Schedule? updatedSchedule = await scheduleStore.UpdateScheduleStatusAsync(schedule.ScheduleId, isActive, nextScheduleOccurrenceDate, currentDate, cancellationToken).ConfigureAwait(false);

        return updatedSchedule ?? throw new KeyNotFoundException($"Scheduler - The schedule '{schedule.ScheduleId}' cannot be found after update");
    }

    public async Task<Schedule> UpdateScheduleRecurrenceConfigsAsync(Guid scheduleId, DateTime? scheduleNextOccurrenceDate, ScheduleRecurrenceTypes scheduleRecurrenceType, DateTime startDate, DateTime? endDate, short everyX, IReadOnlyCollection<Weekdays>? weekdays
        , IReadOnlyCollection<string>? dayOfTheMonths, DateTime currentDate, CancellationToken cancellationToken)
    {
        DateTime? nextOccurrenceDate = schedulerOccurrenceService.GetNextOccurrenceDate(
            scheduleRecurrenceType,
            startDate,
            endDate,
            scheduleNextOccurrenceDate,
            everyX,
            weekdays,
            dayOfTheMonths,
            currentDate);

        Schedule? updatedSchedule = await scheduleStore.UpdateScheduleRecurrenceConfigsAsync(scheduleId, scheduleRecurrenceType, startDate, endDate, everyX, weekdays, dayOfTheMonths, nextOccurrenceDate, currentDate, cancellationToken).ConfigureAwait(false);

        return updatedSchedule ?? throw new KeyNotFoundException($"Scheduler - The schedule '{scheduleId}' cannot be found after update");
    }

    public async Task<Schedule> UpdateScheduleRetryConfigsAsync(Guid scheduleId, bool autoRestartOnFailure, byte retryLimit, DateTime currentDate, CancellationToken cancellationToken)
    {
        Schedule? updatedSchedule = await scheduleStore.UpdateScheduleRetryConfigsAsync(scheduleId, autoRestartOnFailure, retryLimit, currentDate, cancellationToken).ConfigureAwait(false);

        return updatedSchedule ?? throw new KeyNotFoundException($"Scheduler - The schedule '{scheduleId}' cannot be found after update");
    }

    public async Task<Schedule> UpdateScheduleExecutionStatusAsync(Guid scheduleId, ScheduleExecutionStatuses scheduleExecutionStatus, DateTime currentDate, CancellationToken cancellationToken)
    {
        Schedule? updatedSchedule = await scheduleStore.UpdateScheduleExecutionStatusAsync(scheduleId, scheduleExecutionStatus, currentDate, cancellationToken);

        return updatedSchedule ?? throw new KeyNotFoundException($"Scheduler - The schedule '{scheduleId}' cannot be found");
    }

    public async Task UpdateScheduleJobExecutionMetadataAsync(IDictionary<Guid, (LambdaExpression methodCall, Type? explicitType, Type jobLogStoreImplementationType)> scheduleJobExecutionMetadatas, DateTime currentDate, CancellationToken cancellationToken)
    {
        Dictionary<Guid, JobExecutionMetadata> _scheduleJobExecutionMetadatas = [];
        foreach (KeyValuePair<Guid, (LambdaExpression methodCall, Type? explicitType, Type jobLogStoreImplementationType)> keyValuePair in scheduleJobExecutionMetadatas)
        {
            _scheduleJobExecutionMetadatas.Add(keyValuePair.Key, GetJobExecutionMetadata(keyValuePair.Value.methodCall, keyValuePair.Value.explicitType, keyValuePair.Value.jobLogStoreImplementationType));
        }

        IDictionary<Guid, JobExecutionMetadata?> dicWrappers = await scheduleStore.UpdateScheduleJobExecutionMetadataAsync(_scheduleJobExecutionMetadatas, cancellationToken);

        (_, IReadOnlyCollection<Guid> missing) = dicWrappers.PartitionLookupData(_scheduleJobExecutionMetadatas.Keys);

        if (missing.Count != 0)
        {
            throw new KeyNotFoundException($"Scheduler - The following schedules cannot be found after the update: '{string.Join(", ", missing)}'");
        }
    }

    #region Internal mechanism

    public async Task<IEnumerable<Schedule>> UpdateSchedulesExecutionStatusAsync(IReadOnlyCollection<Guid> scheduleIds, ScheduleExecutionStatuses scheduleExecutionStatus, DateTime currentDate, CancellationToken cancellationToken)
    {
        IDictionary<Guid, Schedule?> schedulesDic = await scheduleStore.UpdateSchedulesExecutionStatusAsync(scheduleIds, scheduleExecutionStatus, currentDate, cancellationToken);

        (IReadOnlyCollection<Schedule> found, IReadOnlyCollection<Guid> missing) = schedulesDic.PartitionLookupData(scheduleIds);

        if (missing.Count != 0)
        {
            throw new KeyNotFoundException($"Scheduler - The following schedules cannot be found: '{string.Join(", ", missing)}'");
        }

        return found;
    }

    public async Task<Schedule> UpdateScheduleRetryCountAsync(Guid scheduleId, byte retryCount, DateTime currentDate, CancellationToken cancellationToken)
    {
        DateTime? nextRetryDate = null;
        if (retryCount > 0)
        {
            nextRetryDate = currentDate.AddMinutes(retryCount);
        }

        Schedule? updatedSchedule = await scheduleStore.UpdateScheduleRetryCountAsync(scheduleId, retryCount, nextRetryDate, currentDate, cancellationToken);

        return updatedSchedule ?? throw new KeyNotFoundException($"Scheduler - The schedule '{scheduleId}' cannot be found");
    }

    public async Task<Schedule> UpdateScheduleNextOccurrenceDateAsync(Schedule schedule, DateTime currentDate, CancellationToken cancellationToken)
    {
        DateTime? nextScheduleOccurrenceDate = schedulerOccurrenceService.GetNextOccurrenceDate(
            schedule.ScheduleRecurrenceConfig.ScheduleRecurrenceType,
            schedule.ScheduleRecurrenceConfig.StartDate,
            schedule.ScheduleRecurrenceConfig.EndDate,
            schedule.NextOccurrenceDate,
            schedule.ScheduleRecurrenceConfig.EveryX,
            schedule.ScheduleRecurrenceConfig.Weekdays,
            schedule.ScheduleRecurrenceConfig.DaysOfTheMonth,
            currentDate);

        if (Equals(schedule.NextOccurrenceDate, nextScheduleOccurrenceDate))
        {
            LogScheduleNextOccurenceNoUpdate();
            return schedule;
        }
        
        schedule = (await scheduleStore.UpdateScheduleNextOccurrenceDateAsync(schedule.ScheduleId, nextScheduleOccurrenceDate, currentDate, cancellationToken)) ?? throw new KeyNotFoundException($"Scheduler - The schedule '{schedule.ScheduleId}' cannot be found");

        return schedule;
    }

    public async Task<Schedule> UpdateScheduleLastRunStatusAndDateAsync(Guid scheduleId, ScheduleOccurrenceStatuses lastScheduleOccurrenceStatus, DateTime lastScheduleOccurrenceDate, DateTime currentDate, CancellationToken cancellationToken)
    {
        Schedule? updatedSchedule = await scheduleStore.UpdateScheduleLastRunStatusAndDateAsync(scheduleId, lastScheduleOccurrenceStatus, lastScheduleOccurrenceDate, currentDate, cancellationToken);

        return updatedSchedule ?? throw new KeyNotFoundException($"Scheduler - The schedule '{scheduleId}' cannot be found");
    }

    public async Task<Schedule> UpdateScheduleCurrentRunningBatchIdAsync(Guid scheduleId, Guid? batchId, DateTime currentDate, CancellationToken cancellationToken)
    {
        Schedule? schedule = await scheduleStore.UpdateScheduleCurrentRunningBatchIdAsync(scheduleId, batchId, currentDate, cancellationToken);

        return schedule ?? throw new KeyNotFoundException($"Scheduler - The schedule '{scheduleId}' cannot be found");
    }

    #endregion Internal mechanism

    #endregion Update

    #region Delete

    public async Task DeleteSchedulesAsync(IReadOnlyCollection<Guid> scheduleIds, CancellationToken cancellationToken)
    {
        await scheduleStore.DeleteSchedulesAsync(scheduleIds, cancellationToken).ConfigureAwait(false);
    }

    #endregion Delete

    #region JobExecutionMetadata

    private static readonly ParameterExpression _unusedParameterExpr = Expression.Parameter(typeof(object), "_unused");

    private static JobExecutionMetadata GetJobExecutionMetadata([NotNull] LambdaExpression methodCall, Type? explicitType, Type jobLogStoreImplementationType)
    {
        ArgumentNullException.ThrowIfNull(methodCall);

        MethodCallExpression callExpression = methodCall.Body as MethodCallExpression ?? throw new ArgumentException("Scheduler - Expression body should be of type `MethodCallExpression`", nameof(methodCall));

        Type? type = explicitType ?? callExpression.Method.DeclaringType;

        MethodInfo? method = callExpression.Method;

        if (explicitType == null && callExpression.Object != null)
        {
            object? objectValue = GetExpressionValue(callExpression.Object) ?? throw new InvalidOperationException("Scheduler - Expression object should be not null.");

            type = objectValue.GetType();

            method = type.GetNonOpenMatchingMethod(callExpression.Method.Name, [.. callExpression.Method.GetParameters().Select(static x => x.ParameterType)]);
        }

        if (type == null)
        {
            throw new InvalidOperationException("Scheduler - Type should be not null.");
        }

        if (method == null)
        {
            throw new InvalidOperationException("Scheduler - Method should be not null.");
        }

        List<KeyValuePair<Type, object?>> parameters = [];
        foreach (Expression _expression in callExpression.Arguments)
        {
            parameters.Add(new(_expression.Type, GetExpressionValue(_expression)));
        }

        return new(type, method, parameters.AsReadOnly<KeyValuePair<Type, object?>>(), jobLogStoreImplementationType);
    }

    private static object? GetExpressionValue(Expression expression)
    {
        return TryGetExpressionValue(expression, out object? value) ? value : EvaluateExpression(expression);
    }

    private static bool TryGetExpressionValue(Expression expression, out object? value)
    {
        switch(expression)
        {
            case ConstantExpression constantExpression:
                value = constantExpression.Value;
                return true;

            case MemberExpression { Expression: ConstantExpression targetExpression } memberExpression:
                switch (memberExpression.Member)
                {
                    case FieldInfo fieldInfo:
                        value = fieldInfo.GetValue(targetExpression.Value);
                        return true;
                    case PropertyInfo propertyInfo:
                        value = propertyInfo.GetValue(targetExpression.Value);
                        return true;
                }
                break;
        }

        value = null;
        return false;
    }

    private static object? EvaluateExpression(Expression? expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        Func<object?, object?> func = CompileExpression(expression);

        return func(null);
    }

    private static Func<object?, object?> CompileExpression(Expression expression)
    {
        Expression<Func<object?, object?>> lambdaExpr = Expression.Lambda<Func<object?, object?>>(Expression.Convert(expression, typeof(object)), _unusedParameterExpr);

        return lambdaExpr.Compile();
    }

    #endregion JobExecutionMetadata

    #region Logs

    [LoggerMessage(Level = LogLevel.Debug, Message = "Scheduler - The schedule next occurrence date will not be updated")]
    private partial void LogScheduleNextOccurenceNoUpdate();

    #endregion Logs
}