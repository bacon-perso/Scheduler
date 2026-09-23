using Bacon.Scheduler.Models.Jobs;

namespace Bacon.Scheduler.Samples.Interfaces.Services;

public interface IMaintenanceService
{
    Task<JobExecutionResult> DeleteOldUserTempEmailsAsync(int retentionInSeconds, byte someId);
}