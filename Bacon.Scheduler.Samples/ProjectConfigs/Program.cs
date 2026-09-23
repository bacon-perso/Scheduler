using Bacon.Scheduler.Extensions;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Samples.Interfaces.Services;
using Bacon.Scheduler.Samples.Services;
using Bacon.Scheduler.Samples.Services.Stores;
using Serilog;
using StackExchange.Redis;
using System.Globalization;

WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder(args);

ILoggerFactory loggerFactory = new LoggerFactory();
loggerFactory.AddSerilog();

bool isCrashedPleaseDie = false;
try
{
    webApplicationBuilder.Services.AddControllers();
    webApplicationBuilder.Services.AddOpenApi();

    string tenantId = "schedulertenant";
    string redisServer = webApplicationBuilder.Configuration.GetSection("CacheSettings")["RedisSettings:Server"]!;
    ushort redisPort = Convert.ToUInt16(webApplicationBuilder.Configuration.GetSection("CacheSettings")["RedisSettings:Port"]!, CultureInfo.InvariantCulture);
    string redisPassword = webApplicationBuilder.Configuration.GetSection("CacheSettings")["RedisSettings:Password"]!;
    bool redisUseSsl = Convert.ToBoolean(webApplicationBuilder.Configuration.GetSection("CacheSettings")["RedisSettings:UseSsl"]!);

    ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(new ConfigurationOptions
    {
        AbortOnConnectFail = false,
        AllowAdmin = true,
        ConnectTimeout = 5000,
        ConnectRetry = 5,
        KeepAlive = 60,
        EndPoints = {
                {
                    redisServer,
                    redisPort
                } },
        ReconnectRetryPolicy = new LinearRetry(5000),
        Password = redisPassword,
        Ssl = redisUseSsl,
        SyncTimeout = 5000,
        AsyncTimeout = 5000,
        LoggerFactory = loggerFactory
    });
    //webApplicationBuilder.Services.AddSingleton(connectionMultiplexer);

    webApplicationBuilder.Services.AddSingleton<InMemoryStores>();
    webApplicationBuilder.Services.AddTransient<IMaintenanceService, MaintenanceService>();
    webApplicationBuilder.Services.AddTransient<ICleanUpService, CleanUpService>();

    webApplicationBuilder.Services.AddScheduler<InMemoryScheduleStore, InMemoryScheduleQueueStore, InMemoryBatchStore, InMemoryJobStore>(o =>
    {
        o.TenantId = tenantId;

        o.PeriodicTimerInterval = TimeSpan.FromSeconds(10);

        o.ConcurrencyRateLimiter.ConcurrencyRateLimitProvider = ConcurrencyRateLimitProviders.Redis;
        o.ConcurrencyRateLimiter.ConnectionMultiplexerFactory = (sp) => connectionMultiplexer;
        //o.ConcurrencyRateLimiter.ConnectionMultiplexerFactory = (sp) => sp.GetRequiredService<ConnectionMultiplexer>();
        o.AllowSystemScheduleManualExecution = true;
    })
        .AddOrUpdateSystemSchedule<IMaintenanceService, SystemJobStore>("toto", true, ScheduleRecurrenceTypes.Weekly, new TimeOnly(0, 30, 0), 1, [Weekdays.Saturday], null, true, 3, (i) => i.DeleteOldUserTempEmailsAsync(20, 5))
        .Build();

    WebApplication webApplication = webApplicationBuilder.Build();

    // Configure the HTTP request pipeline.
    if (webApplication.Environment.IsDevelopment())
    {
        webApplication.MapOpenApi();
    }

    webApplication.UseHttpsRedirection();

    webApplication.UseAuthorization();

    webApplication.MapControllers();

    webApplication.Run();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString()); //To log windows event
    //Environment.Exit(1);
    isCrashedPleaseDie = true;
}
finally
{
    if (isCrashedPleaseDie)
    {
        Environment.Exit(1);
    }
}