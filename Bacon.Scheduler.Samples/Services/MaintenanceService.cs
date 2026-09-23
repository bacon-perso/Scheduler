using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Samples.Interfaces.Services;
using Bacon.Scheduler.Samples.Models;

namespace Bacon.Scheduler.Samples.Services;

public class MaintenanceService : IMaintenanceService
{
    public Task<JobExecutionResult> DeleteOldUserTempEmailsAsync(int retentionInSeconds, byte someId)
    {
        Console.WriteLine($"DeleteOldUserTempEmailsAsync ran with {retentionInSeconds} secs and toto is {someId}");

        JobLog jobLog = new()
        {
            DataSourceJobLogId = 1,
            DataSourceId = Guid.NewGuid(),
            RowsInserted = 10,
            RowsUpdated = 5,
            RowsDeleted = 1
        };

        JobExecutionResult jobExecutionResult = new()
        {
            JobExecutionStatus = JobExecutionStatuses.Success,
            Result = jobLog,
            Exceptions = null
        };

        return Task.FromResult(jobExecutionResult);
    }
}