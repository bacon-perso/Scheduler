using Bacon.Scheduler.Models.Queues;

namespace Bacon.Scheduler.Interfaces.Services.Queues;

internal interface IQueueHandlerService
{
    #region Enqueue

    Task EnqueueAsync(ScheduleQueueItemPayload scheduleQueueItemPayload, CancellationToken cancellationToken);

    Task EnqueueAsync(IReadOnlyCollection<ScheduleQueueItemPayload> scheduleQueueItemPayloads, CancellationToken cancellationToken);

    #endregion Enqueue

    #region Run

    Task RunQueueAsync(CancellationToken cancellationToken);

    #endregion Run
}