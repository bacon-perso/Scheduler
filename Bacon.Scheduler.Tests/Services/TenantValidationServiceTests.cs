using Bacon.Scheduler.Services;

namespace Bacon.Scheduler.Tests.Services;

[TestFixture]
public class TenantValidationServiceTests
{
    [TestCase("tenant")]
    [TestCase("a")]
    [TestCase("tenant.name_with-special:chars")]
    public void ValidateTenantId_ValidValues_DoesNotThrow(string tenantId)
        => Assert.DoesNotThrow(() => TenantValidationService.ValidateTenantId(tenantId));

    [Test]
    public void ValidateTenantId_ExactlyFiftyCharacters_DoesNotThrow()
        => Assert.DoesNotThrow(() => TenantValidationService.ValidateTenantId(new string('a', 50)));

    [Test]
    public void ValidateTenantId_Null_ThrowsArgumentException()
        => Assert.Throws<ArgumentException>(() => TenantValidationService.ValidateTenantId(null));

    [Test]
    public void ValidateTenantId_Empty_ThrowsArgumentException()
        => Assert.Throws<ArgumentException>(() => TenantValidationService.ValidateTenantId(""));

    [Test]
    public void ValidateTenantId_FiftyOneCharacters_ThrowsArgumentException()
        => Assert.Throws<ArgumentException>(() => TenantValidationService.ValidateTenantId(new string('a', 51)));

    [TestCase("has spaces")]
    [TestCase("has/slash")]
    [TestCase("has@symbol")]
    public void ValidateTenantId_InvalidCharacters_ThrowsArgumentException(string tenantId)
        => Assert.Throws<ArgumentException>(() => TenantValidationService.ValidateTenantId(tenantId));
}
