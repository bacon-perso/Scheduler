using Bacon.Scheduler.Models.Batches;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Models.Queues;
using Bacon.Scheduler.Models.Schedules;
using System.Collections.Concurrent;

namespace Bacon.Scheduler.Samples.Services.Stores;

public class InMemoryStores
{
    internal readonly ConcurrentDictionary<Guid, Batch> BatchesDic = [];
    internal readonly ConcurrentDictionary<Guid, SchedulerWrapper> SchedulesDic = [];
    internal readonly ConcurrentDictionary<Guid, ScheduleQueueItem> ScheduleQueueItemsDic = [];
    internal readonly ConcurrentDictionary<Guid, Job> JobsDic = [];
}