using Bacon.Scheduler.Interfaces.Services.Queues;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bacon.Scheduler.Services.Queues;

internal sealed partial class QueueHandlerHostedService(IQueueHandlerService queueHandlerService, TimeProvider timeProvider, ILogger<QueueHandlerHostedService> logger) : BackgroundService
{
    //Periodically checks if there is data in the queue to execute
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(10));

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            DateTime currentDate = timeProvider.GetUtcNow().UtcDateTime;

            try
            {
                await queueHandlerService.RunQueueAsync(cancellationToken);
            }
            catch (Exception e)
            {
                LogExecuteError(currentDate, e);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Scheduler - The schedule hosted service had an unexpected error at {CurrentDate:o}")]
    private partial void LogExecuteError(DateTime currentDate, Exception exception);
}