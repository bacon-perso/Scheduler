using Bacon.Scheduler.Interfaces;
using Bacon.Scheduler.Models.Schedules;
using Microsoft.Extensions.DependencyInjection;

namespace Bacon.Scheduler.Services;

internal sealed class SchedulerBuilder(IServiceCollection services, IList<SystemSchedule> systemSchedules) : ISchedulerBuilder
{
    public IServiceCollection Services { get; } = services ?? throw new ArgumentNullException(nameof(services));

    public IList<SystemSchedule> SystemSchedules { get; } = systemSchedules;
}