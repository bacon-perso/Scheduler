using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Jobs;

namespace Bacon.Scheduler.Tests.Fakes;

internal interface ITestJob
{
    Task<JobExecutionResult> RunAsync();

    Task<JobExecutionResult> RunWithArgsAsync(int retentionInSeconds, string label);

    JobExecutionResult RunSync();
}

internal sealed class TestJob : ITestJob
{
    public Task<JobExecutionResult> RunAsync()
        => Task.FromResult(new JobExecutionResult { JobExecutionStatus = JobExecutionStatuses.Success, Result = null, Exceptions = null });

    public Task<JobExecutionResult> RunWithArgsAsync(int retentionInSeconds, string label)
        => Task.FromResult(new JobExecutionResult { JobExecutionStatus = JobExecutionStatuses.Success, Result = null, Exceptions = null });

    public JobExecutionResult RunSync()
        => new() { JobExecutionStatus = JobExecutionStatuses.Success, Result = null, Exceptions = null };
}

internal sealed class TestSettings
{
    public int RetentionInSeconds { get; set; }
}

internal static class TestStaticJob
{
    public static JobExecutionResult RunStatic()
        => new() { JobExecutionStatus = JobExecutionStatuses.Success, Result = null, Exceptions = null };
}

internal sealed class SucceedingQueueJob
{
    public Task<JobExecutionResult> RunAsync()
        => Task.FromResult(new JobExecutionResult { JobExecutionStatus = JobExecutionStatuses.Success, Result = null, Exceptions = null });
}

internal sealed class ThrowingQueueJob
{
    public Task<JobExecutionResult> RunAsync() => throw new InvalidOperationException("boom");
}

internal sealed class ErrorResultQueueJob
{
    public Task<JobExecutionResult> RunAsync()
        => Task.FromResult(new JobExecutionResult { JobExecutionStatus = JobExecutionStatuses.Error, Result = null, Exceptions = null });
}
