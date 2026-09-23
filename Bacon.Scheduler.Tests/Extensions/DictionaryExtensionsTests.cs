using Bacon.Scheduler.Extensions;

namespace Bacon.Scheduler.Tests.Extensions;

[TestFixture]
public class DictionaryExtensionsTests
{
    [Test]
    public void PartitionLookupData_AllKeysFoundWithValues_AllInFound()
    {
        Guid id1 = Guid.NewGuid();
        Guid id2 = Guid.NewGuid();
        IDictionary<Guid, string?> dic = new Dictionary<Guid, string?> { [id1] = "a", [id2] = "b" };

        (IReadOnlyCollection<string> found, IReadOnlyCollection<Guid> missing) = dic.PartitionLookupData([id1, id2]);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.EquivalentTo(new[] { "a", "b" }));
            Assert.That(missing, Is.Empty);
        });
    }

    [Test]
    public void PartitionLookupData_KeyNotInDictionary_GoesToMissing()
    {
        Guid found = Guid.NewGuid();
        Guid absent = Guid.NewGuid();
        IDictionary<Guid, string?> dic = new Dictionary<Guid, string?> { [found] = "a" };

        (IReadOnlyCollection<string> foundValues, IReadOnlyCollection<Guid> missing) = dic.PartitionLookupData([found, absent]);

        Assert.Multiple(() =>
        {
            Assert.That(foundValues, Is.EquivalentTo(new[] { "a" }));
            Assert.That(missing, Is.EquivalentTo(new[] { absent }));
        });
    }

    [Test]
    public void PartitionLookupData_KeyPresentButValueIsNull_GoesToMissing()
    {
        Guid id = Guid.NewGuid();
        IDictionary<Guid, string?> dic = new Dictionary<Guid, string?> { [id] = null };

        (IReadOnlyCollection<string> found, IReadOnlyCollection<Guid> missing) = dic.PartitionLookupData([id]);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.Empty);
            Assert.That(missing, Is.EquivalentTo(new[] { id }));
        });
    }

    [Test]
    public void PartitionLookupData_EmptyRequestedIds_ReturnsEmptyBoth()
    {
        IDictionary<Guid, string?> dic = new Dictionary<Guid, string?> { [Guid.NewGuid()] = "a" };

        (IReadOnlyCollection<string> found, IReadOnlyCollection<Guid> missing) = dic.PartitionLookupData([]);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.Empty);
            Assert.That(missing, Is.Empty);
        });
    }
}
