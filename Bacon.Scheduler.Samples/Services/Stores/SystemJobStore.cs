using Bacon.Scheduler.Interfaces.Services.Jobs.JobLogs;
using Bacon.Scheduler.Models.Jobs;

namespace Bacon.Scheduler.Samples.Services.Stores;

public class SystemJobStore : IJobLogStore
{
    public ValueTask InsertJobLogAsync(Job job, JobExecutionResult jobExecutionResult, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}