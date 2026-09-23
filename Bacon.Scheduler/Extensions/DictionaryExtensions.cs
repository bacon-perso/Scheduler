namespace Bacon.Scheduler.Extensions;

internal static class DictionaryExtensions
{
    public static (IReadOnlyCollection<TValue> found, IReadOnlyCollection<Guid> missing) PartitionLookupData<TValue>(this IDictionary<Guid, TValue?> dic, IReadOnlyCollection<Guid> requestedIds)
    {
        List<Guid> missingIds = [];
        List<TValue> values = [];

        foreach (Guid requestedId in requestedIds)
        {
            dic.TryGetValue(requestedId, out TValue? value);

            if (value is null)
            {
                missingIds.Add(requestedId);
                continue;
            }

            values.Add(value);
        }

        return (values, missingIds);
    }
}