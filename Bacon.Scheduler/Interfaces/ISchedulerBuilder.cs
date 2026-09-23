using Bacon.Scheduler.Models.Schedules;
using Microsoft.Extensions.DependencyInjection;

namespace Bacon.Scheduler.Interfaces;

/// <summary>
/// The scheduler builder
/// </summary>
public interface ISchedulerBuilder
{
    /// <summary>
    /// The service collection
    /// </summary>
    IServiceCollection Services { get; }

    internal IList<SystemSchedule> SystemSchedules { get; }
}