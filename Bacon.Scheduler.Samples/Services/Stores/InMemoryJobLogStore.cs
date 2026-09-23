using Bacon.Scheduler.Interfaces.Services.Jobs.JobLogs;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Samples.Models;
using System.Text.Json;

namespace Bacon.Scheduler.Samples.Services.Stores;

public class InMemoryJobLogStore : IJobLogStore
{
    public ValueTask InsertJobLogAsync(Job job, JobExecutionResult jobExecutionResult, CancellationToken cancellationToken)
    {
        JobLog? jobLog = jobExecutionResult.Result == null ? null : (JobLog)jobExecutionResult.Result;

        Console.WriteLine(JsonSerializer.Serialize(jobLog));

        return ValueTask.CompletedTask;
    }
}