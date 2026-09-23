using Bacon.Scheduler.Interfaces.Services.Queues;
using Bacon.Scheduler.Models.Queues;

namespace Bacon.Scheduler.Tests.Fakes;

internal sealed class FakeQueueHandlerService : IQueueHandlerService
{
    public readonly List<ScheduleQueueItemPayload> EnqueuedSingle = [];
    public readonly List<IReadOnlyCollection<ScheduleQueueItemPayload>> EnqueuedBatches = [];
    public int RunQueueCallCount;

    public Task EnqueueAsync(ScheduleQueueItemPayload scheduleQueueItemPayload, CancellationToken cancellationToken)
    {
        EnqueuedSingle.Add(scheduleQueueItemPayload);
        return Task.CompletedTask;
    }

    public Task EnqueueAsync(IReadOnlyCollection<ScheduleQueueItemPayload> scheduleQueueItemPayloads, CancellationToken cancellationToken)
    {
        EnqueuedBatches.Add(scheduleQueueItemPayloads);
        return Task.CompletedTask;
    }

    public Task RunQueueAsync(CancellationToken cancellationToken)
    {
        RunQueueCallCount++;
        return Task.CompletedTask;
    }
}
