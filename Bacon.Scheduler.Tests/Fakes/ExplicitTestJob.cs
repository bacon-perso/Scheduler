using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Jobs;

namespace Bacon.Scheduler.Tests.Fakes;

internal interface IExplicitTestJob
{
    JobExecutionResult Run();
}

internal sealed class ExplicitTestJob : IExplicitTestJob
{
    JobExecutionResult IExplicitTestJob.Run()
        => new() { JobExecutionStatus = JobExecutionStatuses.Success, Result = null, Exceptions = null };
}
