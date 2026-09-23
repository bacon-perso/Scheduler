namespace Bacon.Scheduler.Models;

internal readonly struct MethodLookupKey(Type type, string name, Type[]? parameterTypes) : IEquatable<MethodLookupKey>
{
    #region Properties

    public Type Type { get; } = type;

    public string Name { get; } = name;

    public Type[]? ParameterTypes { get; } = parameterTypes;

    #endregion Properties

    public bool Equals(MethodLookupKey other) => Type == other.Type && Name.Equals(other.Name, StringComparison.Ordinal) && ParameterTypes.AsSpan().SequenceEqual(other.ParameterTypes);

    public override bool Equals(object? obj) => obj is MethodLookupKey other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Type);
        hash.Add(Name, StringComparer.Ordinal);

        if (ParameterTypes is null)
        {
            return hash.ToHashCode();
        }

        foreach (Type parameterType in ParameterTypes)
        {
            hash.Add(parameterType);
        }

        return hash.ToHashCode();
    }
}