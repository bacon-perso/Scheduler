namespace Bacon.Scheduler.Models.Jobs;

/// <summary>
/// Defines the result of a execution
/// </summary>
public sealed class JobExecutionResult
{
    /// <summary>
    /// Defines the status of the execution
    /// </summary>
    public JobExecutionStatuses JobExecutionStatus { get; init; }

    /// <summary>
    /// Optional result that can be returned from the job
    /// </summary>
    public object? Result { get; init; }

    /// <summary>
    /// Defines all the exceptions trapped by the execution. This will trigger an aggregate exception at run time and set the JobExecutionStatus to Critical
    /// </summary>
    public IEnumerable<Exception>? Exceptions { get; init; }
}