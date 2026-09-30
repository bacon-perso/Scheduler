using Bacon.Scheduler.Models;
using Microsoft.Extensions.Options;

namespace Bacon.Scheduler.Services.Validators;

internal class SchedulerOptionsValidatorService : IValidateOptions<SchedulerOptions>
{
    public ValidateOptionsResult Validate(string? name, SchedulerOptions schedulerOptions)
    {
        List<string> errors = [];

        if (schedulerOptions.PeriodicTimerInterval < TimeSpan.FromSeconds(5) || schedulerOptions.PeriodicTimerInterval > TimeSpan.FromHours(1))
        {
            errors.Add("Scheduler - The periodic timer interval can only contain a value between 5 sec and 1 hour.");
        }

        try
        {
            TenantValidationService.ValidateTenantId(schedulerOptions.TenantId);
        }
        catch
        {
            errors.Add("Scheduler - The tenant ID cannot be null/empty and cannot be longer than 50 characters and only supports the following characters ('A-Z', 'a-z', '0-9', '.', '_', '-', ':'");
        }

        if (schedulerOptions.ConcurrencyRateLimiter is { ConcurrencyRateLimitProvider: ConcurrencyRateLimitProviders.Redis, ConnectionMultiplexerFactory: null })
        {
            errors.Add("Scheduler - The ConnectionMultiplexerFactory cannot be null if the concurrency is 'redis'");
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
