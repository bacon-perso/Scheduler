using Bacon.Scheduler.Interfaces.Services.Batches;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Batches;
using Bacon.Scheduler.Models.Schedules;
using Bacon.Scheduler.Services.Batches;
using Bacon.Scheduler.Tests.Fakes;

namespace Bacon.Scheduler.Tests.Services.Batches;

[TestFixture]
public class BatchServiceTests
{
    private FakeBatchStore _batchStore = null!;
    private IBatchService _service = null!;
    private DateTime _currentDate;

    [SetUp]
    public void Setup()
    {
        _batchStore = new FakeBatchStore();
        _service = new BatchService(_batchStore);
        _currentDate = new DateTime(2026, 7, 30, 8, 0, 0, DateTimeKind.Utc);
    }

    private static Schedule BuildSchedule(Guid scheduleId) => new()
    {
        ScheduleId = scheduleId,
        ScheduleName = "S",
        IsSystem = false,
        IsActive = true,
        ScheduleExecutionStatus = ScheduleExecutionStatuses.Ready,
        LastOccurrenceStatus = ScheduleOccurrenceStatuses.Never,
        CreationDate = DateTime.UtcNow,
        ScheduleRecurrenceConfig = new() { ScheduleRecurrenceType = ScheduleRecurrenceTypes.Daily, StartDate = DateTime.UtcNow, EveryX = 1 },
        ScheduleRetryConfig = new()
    };

    #region Get

    [Test]
    public async Task GetBatchesAsync_AllFound_ReturnsBatches()
    {
        Batch batch = new() { BatchId = Guid.NewGuid(), ScheduleId = Guid.NewGuid(), BatchStartDate = _currentDate };
        _batchStore.Batches[batch.BatchId] = batch;

        IEnumerable<Batch> result = await _service.GetBatchesAsync([batch.BatchId], CancellationToken.None);

        Assert.That(result.Single().BatchId, Is.EqualTo(batch.BatchId));
    }

    [Test]
    public void GetBatchesAsync_SomeMissing_ThrowsKeyNotFoundException()
        => Assert.ThrowsAsync<KeyNotFoundException>(async () => await _service.GetBatchesAsync([Guid.NewGuid()], CancellationToken.None));

    #endregion

    #region Insert

    [Test]
    public async Task InsertBatchAsync_HappyPath_SetsExpectedFields()
    {
        Schedule schedule = BuildSchedule(Guid.NewGuid());

        Batch batch = await _service.InsertBatchAsync(schedule, _currentDate, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(batch.ScheduleId, Is.EqualTo(schedule.ScheduleId));
            Assert.That(batch.BatchStartDate, Is.EqualTo(_currentDate));
            Assert.That(batch.BatchEndDate, Is.Null);
            Assert.That(_batchStore.Batches.ContainsKey(batch.BatchId), Is.True);
        });
    }

    [Test]
    public async Task InsertBatchesAsync_HappyPath_InsertsOneBatchPerSchedule()
    {
        Schedule[] schedules = [BuildSchedule(Guid.NewGuid()), BuildSchedule(Guid.NewGuid())];

        IEnumerable<Batch> batches = await _service.InsertBatchesAsync(schedules, _currentDate, CancellationToken.None);

        Assert.That(batches.Select(b => b.ScheduleId), Is.EquivalentTo(schedules.Select(s => s.ScheduleId)));
    }

    private sealed class DroppingBatchStore : IBatchStore
    {
        public ValueTask<IDictionary<Guid, Batch?>> GetBatchesAsync(IEnumerable<Guid> batchIds, CancellationToken cancellationToken) => ValueTask.FromResult<IDictionary<Guid, Batch?>>(new Dictionary<Guid, Batch?>());
        public ValueTask<Batch?> InsertBatchAsync(Batch batch, CancellationToken cancellationToken) => ValueTask.FromResult<Batch?>(null);
        public ValueTask<IEnumerable<Batch>> InsertBatchesAsync(IEnumerable<Batch> batches, CancellationToken cancellationToken) => ValueTask.FromResult(Enumerable.Empty<Batch>());
        public ValueTask<Batch?> UpdateBatchEndDateAsync(Guid batchId, DateTime currentDate, CancellationToken cancellationToken) => ValueTask.FromResult<Batch?>(null);
        public ValueTask DeleteBatchesByScheduleIdsAsync(IEnumerable<Guid> scheduleIds, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    [Test]
    public void InsertBatchAsync_StoreReturnsNull_ThrowsKeyNotFoundException()
    {
        BatchService service = new(new DroppingBatchStore());
        Schedule schedule = BuildSchedule(Guid.NewGuid());

        Assert.ThrowsAsync<KeyNotFoundException>(async () => await service.InsertBatchAsync(schedule, _currentDate, CancellationToken.None));
    }

    [Test]
    public void InsertBatchesAsync_StoreDropsSomeBatches_ThrowsKeyNotFoundException()
    {
        BatchService service = new(new DroppingBatchStore());
        Schedule[] schedules = [BuildSchedule(Guid.NewGuid())];

        Assert.ThrowsAsync<KeyNotFoundException>(async () => await service.InsertBatchesAsync(schedules, _currentDate, CancellationToken.None));
    }

    #endregion

    #region Update

    [Test]
    public async Task UpdateBatchEndDateAsync_HappyPath_SetsEndDate()
    {
        Batch batch = new() { BatchId = Guid.NewGuid(), ScheduleId = Guid.NewGuid(), BatchStartDate = _currentDate.AddHours(-1) };
        _batchStore.Batches[batch.BatchId] = batch;

        Batch updated = await _service.UpdateBatchEndDateAsync(batch.BatchId, _currentDate, CancellationToken.None);

        Assert.That(updated.BatchEndDate, Is.EqualTo(_currentDate));
    }

    [Test]
    public void UpdateBatchEndDateAsync_NotFound_ThrowsKeyNotFoundException()
        => Assert.ThrowsAsync<KeyNotFoundException>(async () => await _service.UpdateBatchEndDateAsync(Guid.NewGuid(), _currentDate, CancellationToken.None));

    #endregion
}
