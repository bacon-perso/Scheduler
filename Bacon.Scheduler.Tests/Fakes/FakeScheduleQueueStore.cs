using Bacon.Scheduler.Interfaces.Services.Queues;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Queues;
using System.Collections.Concurrent;

namespace Bacon.Scheduler.Tests.Fakes;

internal sealed class FakeScheduleQueueStore : IScheduleQueueStore
{
    public readonly ConcurrentDictionary<Guid, ScheduleQueueItem> Items = [];

    public ValueTask<int> CountAsync(CancellationToken cancellationToken) => ValueTask.FromResult(Items.Count);

    public ValueTask<ScheduleQueueItem?> PeekScheduleQueuePayloadAsync(ScheduleQueueOrders scheduleQueueOrder, CancellationToken cancellationToken)
    {
        ScheduleQueueItem? item = scheduleQueueOrder == ScheduleQueueOrders.OldestFirst
            ? Items.Values.OrderBy(o => o.QueueCreationDate).FirstOrDefault()
            : Items.Values.OrderByDescending(o => o.QueueCreationDate).FirstOrDefault();

        return ValueTask.FromResult(item);
    }

    public ValueTask<IEnumerable<ScheduleQueueItem>> InsertSchedulePayloadsAsync(IEnumerable<ScheduleQueueItemPayload> scheduleQueueItemPayloads, DateTime currentDate, CancellationToken cancellationToken)
    {
        List<ScheduleQueueItem> inserted = [];
        foreach (ScheduleQueueItemPayload payload in scheduleQueueItemPayloads)
        {
            ScheduleQueueItem item = new()
            {
                ScheduleQueueItemId = Guid.NewGuid(),
                Payload = payload,
                QueueCreationDate = currentDate
            };

            if (Items.TryAdd(item.ScheduleQueueItemId, item))
            {
                inserted.Add(item);
            }
        }

        return ValueTask.FromResult(inserted.AsEnumerable());
    }

    public ValueTask DeleteScheduleQueueAsync(Guid scheduleQueueId, CancellationToken cancellationToken)
    {
        Items.TryRemove(scheduleQueueId, out _);
        return ValueTask.CompletedTask;
    }
}
