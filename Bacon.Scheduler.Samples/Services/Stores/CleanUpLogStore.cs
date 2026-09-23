using Bacon.Scheduler.Interfaces.Services.Jobs.JobLogs;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Samples.Models;
using System.Text.Json;

namespace Bacon.Scheduler.Samples.Services.Stores;

public class CleanUpLogStore : IJobLogStore
{
    public ValueTask InsertJobLogAsync(Job job, JobExecutionResult jobExecutionResult, CancellationToken cancellationToken)
    {
        CleanUpLog? cleanUpLog = jobExecutionResult.Result == null ? null : (CleanUpLog)jobExecutionResult.Result;

        Console.WriteLine(JsonSerializer.Serialize(cleanUpLog));

        return ValueTask.CompletedTask;
    }
}