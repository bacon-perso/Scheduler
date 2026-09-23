using System.Reflection;

namespace Bacon.Scheduler.Extensions;

internal static class MethodInfoExtensions
{
    public static string GetNormalizedName(this MethodInfo methodInfo)
    {
        // Method names containing '.' are considered explicitly implemented interface methods
        // https://stackoverflow.com/a/17854048/1398672
        return methodInfo.Name.Contains('.') && methodInfo is { IsFinal: true, IsPrivate: true } ? methodInfo.Name.Split('.').Last() : methodInfo.Name;
    }
}