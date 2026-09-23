using Bacon.Scheduler.Interfaces.Services.Jobs.JobLogs;
using Bacon.Scheduler.Models.Jobs;
using System.Collections.Concurrent;

namespace Bacon.Scheduler.Tests.Fakes;

/// <summary>
/// QueueHandlerService resolves IJobLogStore implementations via ActivatorUtilities.CreateInstance
/// per job execution, so tests can't inject a specific instance. Calls are recorded in a static,
/// concurrent collection instead; tests using it should clear <see cref="InsertedLogs"/> in [SetUp].
/// </summary>
internal sealed class FakeJobLogStore : IJobLogStore
{
    public static readonly ConcurrentQueue<(Job Job, JobExecutionResult Result)> InsertedLogs = [];

    public ValueTask InsertJobLogAsync(Job job, JobExecutionResult jobExecutionResult, CancellationToken cancellationToken)
    {
        InsertedLogs.Enqueue((job, jobExecutionResult));
        return ValueTask.CompletedTask;
    }
}
