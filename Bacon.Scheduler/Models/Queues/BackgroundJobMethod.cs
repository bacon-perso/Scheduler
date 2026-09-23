using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Bacon.Scheduler.Models.Queues;

internal sealed class BackgroundJobMethod(MethodInfo methodInfo, object instance, object?[] parameters)
{
    public object? Result { get; private set; }

    public object? Invoke()
    {
        try
        {
            Result = methodInfo.Invoke(instance, parameters);
            return Result;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }
}