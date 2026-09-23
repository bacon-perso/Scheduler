using Bacon.Scheduler.Models.Jobs;

namespace Bacon.Scheduler.Samples.Interfaces.Services;

public interface ICleanUpService
{
    Task<JobExecutionResult> CleanUpAsync(int retentionInSeconds);
}