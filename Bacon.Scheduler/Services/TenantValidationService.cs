using System.Text.RegularExpressions;

namespace Bacon.Scheduler.Services;

internal static partial class TenantValidationService
{
    public static readonly string TenantErrorMessage = "Scheduler - The tenant ID cannot be null/empty and cannot be longer than 50 characters and only supports the following characters ('A-Z', 'a-z', '0-9', '.', '_', '-', ':'";

    public static void ValidateTenantId(string? tenantId)
    {
        if (tenantId is null || !TenantValidationRegex().IsMatch(tenantId))
        {
            throw new ArgumentException(TenantErrorMessage, nameof(tenantId));
        }
    }

    [GeneratedRegex("^[A-Za-z0-9._:-]{1,50}$")]
    private static partial Regex TenantValidationRegex();
}