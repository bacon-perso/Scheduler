using Bacon.Scheduler.Interfaces.Services.Queues;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Queues;

namespace Bacon.Scheduler.Samples.Services.Stores;

public class InMemoryScheduleQueueStore(InMemoryStores inMemoryStores, ILogger<InMemoryScheduleQueueStore> logger) : IScheduleQueueStore
{
    #region Get

    public ValueTask<int> CountAsync(CancellationToken cancellationToken)
    {
        int count = inMemoryStores.ScheduleQueueItemsDic.Count;

        return ValueTask.FromResult(count);
    }

    public ValueTask<ScheduleQueueItem?> PeekScheduleQueuePayloadAsync(ScheduleQueueOrders scheduleQueueOrder, CancellationToken cancellationToken)
    {
        KeyValuePair<Guid, ScheduleQueueItem> keyValuePair = scheduleQueueOrder.Equals(ScheduleQueueOrders.OldestFirst) ? inMemoryStores.ScheduleQueueItemsDic.OrderByDescending(obd => obd.Value.QueueCreationDate).FirstOrDefault() : inMemoryStores.ScheduleQueueItemsDic.OrderBy(obd => obd.Value.QueueCreationDate).FirstOrDefault();

        ScheduleQueueItem? scheduleQueueItem = null;

        if (!keyValuePair.Equals(default(KeyValuePair<Guid, ScheduleQueueItem>)))
        {
            scheduleQueueItem = keyValuePair.Value;
        }

        return ValueTask.FromResult(scheduleQueueItem);
    }

    #endregion Get

    #region Insert

    public ValueTask<IEnumerable<ScheduleQueueItem>> InsertSchedulePayloadsAsync(IEnumerable<ScheduleQueueItemPayload> scheduleQueueItemPayloads, DateTime currentDate, CancellationToken cancellationToken)
    {
        List<ScheduleQueueItem> scheduleQueueItems = [];
        foreach (ScheduleQueueItemPayload scheduleQueueItemPayload in scheduleQueueItemPayloads)
        {
            Guid scheduleQueueId = Guid.NewGuid();

            ScheduleQueueItem scheduleQueueItem = new()
            {
                ScheduleQueueItemId = scheduleQueueId,
                Payload = scheduleQueueItemPayload,
                QueueCreationDate = currentDate
            };

            if (inMemoryStores.ScheduleQueueItemsDic.TryAdd(scheduleQueueId, scheduleQueueItem))
            {
                scheduleQueueItems.Add(scheduleQueueItem);
            }
            else
            {
                logger.LogError("Error inserting queueId {ScheduleQueueId} with value {ScheduleQueue}", scheduleQueueId, scheduleQueueItem);
            }
        }

        return ValueTask.FromResult(scheduleQueueItems.AsEnumerable());
    }

    #endregion Insert

    #region Delete

    public ValueTask DeleteScheduleQueueAsync(Guid scheduleQueueId, CancellationToken cancellationToken)
    {
        inMemoryStores.ScheduleQueueItemsDic.TryRemove(scheduleQueueId, out _);

        return ValueTask.CompletedTask;
    }

    #endregion Delete
}