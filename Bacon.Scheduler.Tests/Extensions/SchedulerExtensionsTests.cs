using Bacon.Scheduler.Extensions;
using Bacon.Scheduler.Interfaces;
using Bacon.Scheduler.Interfaces.Services.Batches;
using Bacon.Scheduler.Interfaces.Services.Jobs;
using Bacon.Scheduler.Interfaces.Services.Jobs.JobLogs;
using Bacon.Scheduler.Interfaces.Services.Queues;
using Bacon.Scheduler.Interfaces.Services.RateLimiters;
using Bacon.Scheduler.Interfaces.Services.Schedules;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Models.Schedules;
using Bacon.Scheduler.Services.Queues;
using Bacon.Scheduler.Services.RateLimiters;
using Bacon.Scheduler.Services.Schedules;
using Bacon.Scheduler.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;
using StackExchange.Redis;

namespace Bacon.Scheduler.Tests.Extensions;

[TestFixture]
public class SchedulerExtensionsTests
{
    private static readonly Expression<Func<ITestJob, Task<JobExecutionResult>>> _asyncMethodCall = i => i.RunAsync();
    private static readonly Expression<Func<TestJob, JobExecutionResult>> _syncMethodCall = i => i.RunSync();

    private static ServiceCollection NewServices() => [];

    #region AddScheduler validation

    [Test]
    public void AddScheduler_ValidOptions_RegistersStoresAndReturnsBuilder()
    {
        ServiceCollection services = NewServices();

        ISchedulerBuilder builder = services.AddScheduler<FakeScheduleStore, FakeScheduleQueueStore, FakeBatchStore, FakeJobStore>(o =>
        {
            o.TenantId = "test-tenant";
        });

        Assert.Multiple(() =>
        {
            Assert.That(builder, Is.Not.Null);
            Assert.That(builder.Services, Is.SameAs(services));
            Assert.That(builder.SystemSchedules, Is.Empty);
            Assert.That(services.Any(d => d.ServiceType == typeof(IScheduleStore) && d.ImplementationType == typeof(FakeScheduleStore)), Is.True);
            Assert.That(services.Any(d => d.ServiceType == typeof(IScheduleQueueStore) && d.ImplementationType == typeof(FakeScheduleQueueStore)), Is.True);
            Assert.That(services.Any(d => d.ServiceType == typeof(IBatchStore) && d.ImplementationType == typeof(FakeBatchStore)), Is.True);
            Assert.That(services.Any(d => d.ServiceType == typeof(IJobStore) && d.ImplementationType == typeof(FakeJobStore)), Is.True);
        });
    }

    // Options are validated by ValidateOnStart, so failures surface when the startup validator runs rather than from AddScheduler.
    private static OptionsValidationException ValidateOnStartExpectingFailure(Action<SchedulerOptions> configure)
    {
        ServiceCollection services = NewServices();
        services.AddScheduler<FakeScheduleStore, FakeScheduleQueueStore, FakeBatchStore, FakeJobStore>(configure);

        using ServiceProvider provider = services.BuildServiceProvider();

        return Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IStartupValidator>().Validate())!;
    }

    private static void AssertStartupValidationPasses(Action<SchedulerOptions> configure)
    {
        ServiceCollection services = NewServices();
        services.AddScheduler<FakeScheduleStore, FakeScheduleQueueStore, FakeBatchStore, FakeJobStore>(configure);

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStartupValidator>().Validate());
    }

    [Test]
    public void AddScheduler_InvalidOptions_DoesNotThrowUntilStartupValidation()
    {
        ServiceCollection services = NewServices();

        Assert.DoesNotThrow(() =>
            services.AddScheduler<FakeScheduleStore, FakeScheduleQueueStore, FakeBatchStore, FakeJobStore>(o => o.TenantId = ""));
    }

    [Test]
    public void AddScheduler_PeriodicTimerIntervalBelowFiveSeconds_FailsStartupValidation()
    {
        OptionsValidationException ex = ValidateOnStartExpectingFailure(o =>
        {
            o.TenantId = "test-tenant";
            o.PeriodicTimerInterval = TimeSpan.FromSeconds(1);
        });

        Assert.That(ex.Failures, Has.One.Contain("periodic timer interval"));
    }

    [Test]
    public void AddScheduler_PeriodicTimerIntervalAboveOneHour_FailsStartupValidation()
    {
        OptionsValidationException ex = ValidateOnStartExpectingFailure(o =>
        {
            o.TenantId = "test-tenant";
            o.PeriodicTimerInterval = TimeSpan.FromHours(2);
        });

        Assert.That(ex.Failures, Has.One.Contain("periodic timer interval"));
    }

    [TestCase(5)]
    [TestCase(60)]
    [TestCase(3600)]
    public void AddScheduler_PeriodicTimerIntervalWithinBounds_PassesStartupValidation(int seconds)
    {
        AssertStartupValidationPasses(o =>
        {
            o.TenantId = "test-tenant";
            o.PeriodicTimerInterval = TimeSpan.FromSeconds(seconds);
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("has spaces")]
    [TestCase("has/slash")]
    public void AddScheduler_InvalidTenantId_FailsStartupValidation(string? tenantId)
    {
        OptionsValidationException ex = ValidateOnStartExpectingFailure(o => o.TenantId = tenantId!);

        Assert.That(ex.Failures, Has.One.Contain("tenant ID"));
    }

    [Test]
    public void AddScheduler_TenantIdLongerThan50Characters_FailsStartupValidation()
    {
        string tooLong = new('a', 51);

        OptionsValidationException ex = ValidateOnStartExpectingFailure(o => o.TenantId = tooLong);

        Assert.That(ex.Failures, Has.One.Contain("tenant ID"));
    }

    [Test]
    public void AddScheduler_RedisProviderWithoutConnectionFactory_FailsStartupValidation()
    {
        OptionsValidationException ex = ValidateOnStartExpectingFailure(o =>
        {
            o.TenantId = "test-tenant";
            o.ConcurrencyRateLimiter.ConcurrencyRateLimitProvider = ConcurrencyRateLimitProviders.Redis;
        });

        Assert.That(ex.Failures, Has.One.Contain("ConnectionMultiplexerFactory"));
    }

    [Test]
    public void AddScheduler_RedisProviderWithConnectionFactory_PassesStartupValidation()
    {
        AssertStartupValidationPasses(o =>
        {
            o.TenantId = "test-tenant";
            o.ConcurrencyRateLimiter.ConcurrencyRateLimitProvider = ConcurrencyRateLimitProviders.Redis;
            o.ConcurrencyRateLimiter.ConnectionMultiplexerFactory = _ => default(IConnectionMultiplexer)!;
        });
    }

    [Test]
    public void AddScheduler_MultipleInvalidOptions_ReportsEveryFailure()
    {
        OptionsValidationException ex = ValidateOnStartExpectingFailure(o =>
        {
            o.TenantId = "";
            o.PeriodicTimerInterval = TimeSpan.FromSeconds(1);
            o.ConcurrencyRateLimiter.ConcurrencyRateLimitProvider = ConcurrencyRateLimitProviders.Redis;
        });

        Assert.That(ex.Failures.Count(), Is.EqualTo(3));
    }

    [TestCase(ConcurrencyRateLimitProviders.Local, typeof(LocalRateLimiterService))]
    [TestCase(ConcurrencyRateLimitProviders.Redis, typeof(RedisRateLimiterService))]
    public void AddScheduler_ConcurrencyProvider_RegistersMatchingLimiter(ConcurrencyRateLimitProviders provider, Type expectedImplementation)
    {
        ServiceCollection services = NewServices();

        services.AddScheduler<FakeScheduleStore, FakeScheduleQueueStore, FakeBatchStore, FakeJobStore>(o =>
        {
            o.TenantId = "test-tenant";
            o.ConcurrencyRateLimiter.ConcurrencyRateLimitProvider = provider;
        });

        Assert.That(services.Any(d => d.ServiceType == typeof(IConcurrencyRateLimiterService) && d.ImplementationType == expectedImplementation), Is.True);
    }

    #endregion

    #region AddOrUpdateSystemSchedule

    [Test]
    public void AddOrUpdateSystemSchedule_AsyncOverload_AddsSystemSchedule()
    {
        ServiceCollection services = NewServices();
        ISchedulerBuilder builder = services.AddScheduler<FakeScheduleStore, FakeScheduleQueueStore, FakeBatchStore, FakeJobStore>(o => o.TenantId = "test-tenant");

        ISchedulerBuilder result = builder.AddOrUpdateSystemSchedule<ITestJob, FakeJobLogStore>("Maintenance", true, ScheduleRecurrenceTypes.Daily, new TimeOnly(1, 0), 1, null, null, false, 3, _asyncMethodCall);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(builder));
            Assert.That(builder.SystemSchedules, Has.Count.EqualTo(1));
            Assert.That(builder.SystemSchedules[0].ScheduleName, Is.EqualTo("Maintenance"));
            Assert.That(builder.SystemSchedules[0].JobType, Is.EqualTo(typeof(ITestJob)));
            Assert.That(builder.SystemSchedules[0].JobLogStoreImplementationType, Is.EqualTo(typeof(FakeJobLogStore)));
        });
    }

    [Test]
    public void AddOrUpdateSystemSchedule_SyncOverload_AddsSystemSchedule()
    {
        ServiceCollection services = NewServices();
        ISchedulerBuilder builder = services.AddScheduler<FakeScheduleStore, FakeScheduleQueueStore, FakeBatchStore, FakeJobStore>(o => o.TenantId = "test-tenant");

        builder.AddOrUpdateSystemSchedule<TestJob, FakeJobLogStore>("SyncMaintenance", true, ScheduleRecurrenceTypes.Hourly, new TimeOnly(1, 0), 1, null, null, false, 3, _syncMethodCall);

        Assert.That(builder.SystemSchedules, Has.Count.EqualTo(1));
        Assert.That(builder.SystemSchedules[0].ScheduleName, Is.EqualTo("SyncMaintenance"));
    }

    [Test]
    public void AddOrUpdateSystemSchedule_CalledMultipleTimes_AccumulatesInOrder()
    {
        ServiceCollection services = NewServices();
        ISchedulerBuilder builder = services.AddScheduler<FakeScheduleStore, FakeScheduleQueueStore, FakeBatchStore, FakeJobStore>(o => o.TenantId = "test-tenant")
            .AddOrUpdateSystemSchedule<ITestJob, FakeJobLogStore>("First", true, ScheduleRecurrenceTypes.Daily, new TimeOnly(1, 0), 1, null, null, false, 3, _asyncMethodCall)
            .AddOrUpdateSystemSchedule<ITestJob, FakeJobLogStore>("Second", true, ScheduleRecurrenceTypes.Daily, new TimeOnly(2, 0), 1, null, null, false, 3, _asyncMethodCall);

        Assert.That(builder.SystemSchedules.Select(s => s.ScheduleName), Is.EqualTo(new[] { "First", "Second" }));
    }

    #endregion

    #region Build

    [Test]
    public void Build_RegistersBothHostedServicesAndCoreInternalServices()
    {
        ServiceCollection services = NewServices();
        ISchedulerBuilder builder = services.AddScheduler<FakeScheduleStore, FakeScheduleQueueStore, FakeBatchStore, FakeJobStore>(o => o.TenantId = "test-tenant");

        builder.Build();

        Assert.Multiple(() =>
        {
            Assert.That(services.Any(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(SchedulerHostedService)), Is.True);
            Assert.That(services.Any(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(QueueHandlerHostedService)), Is.True);
            Assert.That(services.Any(d => d.ServiceType == typeof(IScheduleService) && d.ImplementationType == typeof(ScheduleService)), Is.True);
            Assert.That(services.Any(d => d.ServiceType == typeof(IInternalScheduleService)), Is.True);
            Assert.That(services.Any(d => d.ServiceType == typeof(IBatchService)), Is.True);
            Assert.That(services.Any(d => d.ServiceType == typeof(IJobService)), Is.True);
            Assert.That(services.Any(d => d.ServiceType == typeof(IScheduleValidationService)), Is.True);
            Assert.That(services.Any(d => d.ServiceType == typeof(ISchedulerOccurrenceService)), Is.True);
            Assert.That(services.Any(d => d.ServiceType == typeof(IQueueHandlerService)), Is.True);
            Assert.That(services.Any(d => d.ServiceType == typeof(TimeProvider)), Is.True);
        });
    }

    [Test]
    public void Build_DuplicateSystemScheduleNames_ThrowsArgumentException()
    {
        ServiceCollection services = NewServices();
        ISchedulerBuilder builder = services.AddScheduler<FakeScheduleStore, FakeScheduleQueueStore, FakeBatchStore, FakeJobStore>(o => o.TenantId = "test-tenant")
            .AddOrUpdateSystemSchedule<ITestJob, FakeJobLogStore>("Maintenance", true, ScheduleRecurrenceTypes.Daily, new TimeOnly(1, 0), 1, null, null, false, 3, _asyncMethodCall)
            .AddOrUpdateSystemSchedule<ITestJob, FakeJobLogStore>("maintenance", true, ScheduleRecurrenceTypes.Daily, new TimeOnly(2, 0), 1, null, null, false, 3, _asyncMethodCall); // same name, case-insensitive

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Test]
    public async Task Build_ThenResolveFromContainer_IScheduleServiceIsResolvable()
    {
        ServiceCollection services = NewServices();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        services.AddScheduler<FakeScheduleStore, FakeScheduleQueueStore, FakeBatchStore, FakeJobStore>(o => o.TenantId = "test-tenant")
            .Build();

        // LocalRateLimiterService (registered via AddActivatedSingleton) is IAsyncDisposable-only, so the provider itself must be disposed asynchronously.
        await using ServiceProvider provider = services.BuildServiceProvider();

        IScheduleService scheduleService = provider.GetRequiredService<IScheduleService>();
        SchedulerOptions options = provider.GetRequiredService<IOptions<SchedulerOptions>>().Value;
        IEnumerable<Type> hostedServiceTypes = provider.GetServices<IHostedService>().Select(s => s.GetType());

        Assert.Multiple(() =>
        {
            Assert.That(scheduleService, Is.Not.Null);
            Assert.That(options.TenantId, Is.EqualTo("test-tenant"));
            // AddActivatedSingleton also contributes its own AutoActivationHostedService alongside the two scheduler hosted services, so check membership rather than exact equivalence.
            Assert.That(hostedServiceTypes, Does.Contain(typeof(SchedulerHostedService)));
            Assert.That(hostedServiceTypes, Does.Contain(typeof(QueueHandlerHostedService)));
        });
    }

    #endregion
}
