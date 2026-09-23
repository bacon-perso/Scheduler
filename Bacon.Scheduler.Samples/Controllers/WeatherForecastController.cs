using Bacon.Scheduler.Interfaces.Services.Schedules;
using Bacon.Scheduler.Models;
using Bacon.Scheduler.Models.Jobs;
using Bacon.Scheduler.Models.Schedules;
using Bacon.Scheduler.Samples.Interfaces.Services;
using Bacon.Scheduler.Samples.Models;
using Bacon.Scheduler.Samples.Services.Stores;
using Microsoft.AspNetCore.Mvc;

namespace Bacon.Scheduler.Samples.Controllers;

[ApiController]
[Route("api/schedules")]
public class WeatherForecastController(IScheduleService schedulerService, IScheduleStore scheduleStore, IMaintenanceService maintenanceService, ICleanUpService cleanUpService) : ControllerBase
{
    [HttpGet]
    [Route("{scheduleId}")]
    public async Task<IActionResult> GetScheduleAsync([FromRoute] Guid scheduleId, CancellationToken cancellationToken)
    {
        Schedule? schedule = await scheduleStore.GetScheduleAsync(scheduleId, cancellationToken);

        if (schedule == null)
        {
            return NotFound();
        }

        return Ok(schedule);
    }

    [HttpPost]
    [Route("{name}")]
    public async Task<IActionResult> InsertScheduleAsync([FromRoute] string name, CancellationToken cancellationToken)
    {
        MaintenanceSettings maintenanceSettings = new();

        DateTime dateNow = DateTime.UtcNow;

        Schedule schedule = default!;
        if (name.Equals("1", StringComparison.Ordinal))
        {
            schedule = await schedulerService.InsertScheduleAsync<InMemoryJobLogStore>(name, ScheduleRecurrenceTypes.Hourly, null, null, 1, null, null, false, 1, () => maintenanceService.DeleteOldUserTempEmailsAsync(maintenanceSettings.UserTempEmailRetentionInSeconds, 2), cancellationToken);
        }
        else
        {
            schedule = await schedulerService.InsertScheduleAsync<CleanUpLogStore>(name, ScheduleRecurrenceTypes.Hourly, null, null, 1, null, null, false, 1, () => cleanUpService.CleanUpAsync(5), cancellationToken);
        }

        return Ok(schedule);
    }

    [HttpPost]
    [Route("{scheduleId}/run")]
    public async Task<IActionResult> RunScheduleAsync([FromRoute] Guid scheduleId, CancellationToken cancellationToken)
    {
        Schedule? schedule = await scheduleStore.GetScheduleAsync(scheduleId, cancellationToken);

        if (schedule == null)
        {
            return NotFound();
        }

        Job job = await schedulerService.RunScheduleAsync(schedule.ScheduleId, cancellationToken);

        return Ok(job);
    }
}
