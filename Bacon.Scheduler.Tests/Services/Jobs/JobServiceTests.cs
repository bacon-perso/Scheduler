using Bacon.Scheduler.Interfaces.Services.Jobs;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Batches;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Services.Jobs;
using Bacon.Scheduler.Tests.Fakes;

namespace Bacon.Scheduler.Tests.Services.Jobs;

[TestFixture]
public class JobServiceTests
{
    private FakeJobStore _jobStore = null!;
    private IJobService _service = null!;
    private DateTime _currentDate;

    [SetUp]
    public void Setup()
    {
        _jobStore = new FakeJobStore();
        _service = new JobService(_jobStore);
        _currentDate = new DateTime(2026, 7, 30, 8, 0, 0, DateTimeKind.Utc);
    }

    #region Insert

    [Test]
    public async Task InsertJobAsync_HappyPath_InheritsBatchStartDateAndQueuedStatus()
    {
        Batch batch = new() { BatchId = Guid.NewGuid(), ScheduleId = Guid.NewGuid(), BatchStartDate = _currentDate };

        Job job = await _service.InsertJobAsync(batch, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(job.BatchId, Is.EqualTo(batch.BatchId));
            Assert.That(job.JobStartDate, Is.EqualTo(batch.BatchStartDate));
            Assert.That(job.JobExecutionStatus, Is.EqualTo(JobExecutionStatuses.Queued));
            Assert.That(job.JobEndDate, Is.Null);
            Assert.That(_jobStore.Jobs.ContainsKey(job.JobId), Is.True);
        });
    }

    [Test]
    public async Task InsertJobsAsync_HappyPath_InsertsOneJobPerBatch()
    {
        Guid[] batchIds = [Guid.NewGuid(), Guid.NewGuid()];

        IEnumerable<Job> jobs = await _service.InsertJobsAsync(batchIds, _currentDate, CancellationToken.None);

        Assert.That(jobs.Select(j => j.BatchId), Is.EquivalentTo(batchIds));
    }

    private sealed class DroppingJobStore : IJobStore
    {
        public ValueTask<Job?> InsertJobAsync(Job job, CancellationToken cancellationToken) => ValueTask.FromResult<Job?>(null);
        public ValueTask<IEnumerable<Job>> InsertJobsAsync(IEnumerable<Job> jobs, CancellationToken cancellationToken) => ValueTask.FromResult(Enumerable.Empty<Job>());
        public ValueTask<Job?> UpdateJobExecutionStatusAsync(Guid jobId, JobExecutionStatuses jobExecutionStatus, DateTime? jobEndDate, DateTime currentDate, CancellationToken cancellationToken) => ValueTask.FromResult<Job?>(null);
        public ValueTask DeleteJobsByBatchIdsAsync(IEnumerable<Guid> batchIds, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    [Test]
    public void InsertJobAsync_StoreReturnsNull_ThrowsKeyNotFoundException()
    {
        JobService service = new(new DroppingJobStore());
        Batch batch = new() { BatchId = Guid.NewGuid(), ScheduleId = Guid.NewGuid(), BatchStartDate = _currentDate };

        Assert.ThrowsAsync<KeyNotFoundException>(async () => await service.InsertJobAsync(batch, CancellationToken.None));
    }

    [Test]
    public void InsertJobsAsync_StoreDropsSomeJobs_ThrowsKeyNotFoundException()
    {
        JobService service = new(new DroppingJobStore());

        Assert.ThrowsAsync<KeyNotFoundException>(async () => await service.InsertJobsAsync([Guid.NewGuid()], _currentDate, CancellationToken.None));
    }

    #endregion

    #region Update

    [TestCase(JobExecutionStatuses.Success, true)]
    [TestCase(JobExecutionStatuses.Error, true)]
    [TestCase(JobExecutionStatuses.Critical, true)]
    [TestCase(JobExecutionStatuses.Running, false)]
    [TestCase(JobExecutionStatuses.Queued, false)]
    public async Task UpdateJobExecutionStatusAsync_TerminalStatuses_SetJobEndDate(JobExecutionStatuses status, bool expectEndDateSet)
    {
        Job job = new() { JobId = Guid.NewGuid(), BatchId = Guid.NewGuid(), JobExecutionStatus = JobExecutionStatuses.Running, JobStartDate = _currentDate, CreationDate = _currentDate };
        _jobStore.Jobs[job.JobId] = job;

        Job updated = await _service.UpdateJobExecutionStatusAsync(job.JobId, status, _currentDate, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(updated.JobExecutionStatus, Is.EqualTo(status));
            Assert.That(updated.JobEndDate, expectEndDateSet ? Is.EqualTo(_currentDate) : Is.Null);
        });
    }

    [Test]
    public void UpdateJobExecutionStatusAsync_NotFound_ThrowsKeyNotFoundException()
        => Assert.ThrowsAsync<KeyNotFoundException>(async () => await _service.UpdateJobExecutionStatusAsync(Guid.NewGuid(), JobExecutionStatuses.Success, _currentDate, CancellationToken.None));

    #endregion
}
