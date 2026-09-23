using System.Reflection;
using System.Text.Json.Serialization;

namespace Bacon.Scheduler.Models.Jobs;

/// <summary>
/// Defines the metadata of the job
/// </summary>
/// <param name="type">The type of class to activate</param>
/// <param name="methodInfo">The method info to execute</param>
/// <param name="args">The list of parameters to pass</param>
/// <param name="jobLogStoreImplementationType">TThe type of class to activate for the job log store</param>
public sealed class JobExecutionMetadata(Type type, MethodInfo methodInfo, IReadOnlyList<KeyValuePair<Type, object?>> args, Type jobLogStoreImplementationType)
{
    /// <summary>
    /// Gets the metadata of a type that contains a method that should be 
    /// invoked during the performance.
    /// </summary>
    [JsonPropertyName("type")]
    public Type JobExecutionImplementationType { get; } = type;

    /// <summary>
    /// Gets the metadata of a method that should be invoked during the 
    /// performance.
    /// </summary>
    public MethodInfo Method { get; } = methodInfo;

    /// <summary>
    /// Gets a read-only collection of arguments that Should be passed to a 
    /// method invocation during the performance.
    /// </summary>
    public IReadOnlyList<KeyValuePair<Type, object?>> Arguments { get; } = args;

    /// <summary>
    /// Defines the type of the implementation used in the job log store
    /// </summary>
    public Type JobLogStoreImplementationType { get; } = jobLogStoreImplementationType;
}