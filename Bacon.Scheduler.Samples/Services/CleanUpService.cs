using Bacon.Scheduler.Interfaces.Services.Schedules;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Samples.Interfaces.Services;
using Bacon.Scheduler.Samples.Models;
using Bacon.Scheduler.Samples.Services.Stores;

namespace Bacon.Scheduler.Samples.Services;

public class CleanUpService(IScheduleService scheduleService) : ICleanUpService
{
    public async Task<JobExecutionResult> CleanUpAsync(int retentionInSeconds)
    {
        CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        Console.WriteLine($"DeleteOldUserTempEmailsAsync ran with {retentionInSeconds} secs");

        CleanUpLog cleanUpLog = new()
        {
            CleanUpLogId = 1,
            TotoId = Guid.NewGuid()
        };

        JobExecutionResult jobExecutionResult = new()
        {
            JobExecutionStatus = JobExecutionStatuses.Success,
            Result = cleanUpLog,
            Exceptions = null
        };

        await scheduleService.InsertDelayedScheduleAsync<CleanUpLogStore>(TimeSpan.FromSeconds(10), () => WriteLog(), cancellationToken);

        return jobExecutionResult;
    }

    public JobExecutionResult WriteLog()
    {
        Console.WriteLine("toto");

        return new()
        {
            JobExecutionStatus = JobExecutionStatuses.Success,
            Result = null,
            Exceptions = null
        };
    }
}