using Bacon.Scheduler.Models;
using System.Collections.Concurrent;
using System.Reflection;

namespace Bacon.Scheduler.Extensions;

internal static class TypeExtensions
{
    private static readonly ConcurrentDictionary<MethodLookupKey, MethodInfo?> _methodCache = new();

    public static MethodInfo? GetNonOpenMatchingMethod(this Type type, string name, Type[]? parameterTypes)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(name);

        parameterTypes ??= Type.EmptyTypes;

        return _methodCache.GetOrAdd(new MethodLookupKey(type, name, parameterTypes), static key => key.ResolveMethod());
    }

    private static MethodInfo? ResolveMethod(this MethodLookupKey methodLookupKey)
    {
        List<MethodInfo> methodCandidates = [.. methodLookupKey.Type.GetRuntimeMethods()];

        if (methodLookupKey.Type.GetTypeInfo().IsInterface)
        {
            methodCandidates.AddRange(methodLookupKey.Type.GetTypeInfo().ImplementedInterfaces.SelectMany(static sm => sm.GetRuntimeMethods()));
        }

        foreach (MethodInfo methodCandidate in methodCandidates)
        {
            if (!methodCandidate.GetNormalizedName().Equals(methodLookupKey.Name, StringComparison.Ordinal))
            {
                continue;
            }

            ParameterInfo[] parameters = methodCandidate.GetParameters();
            if (parameters.Length != methodLookupKey.ParameterTypes?.Length)
            {
                continue;
            }

            bool parameterTypesMatched = true;

            Type?[]? genericArguments = methodCandidate.ContainsGenericParameters ? new Type[methodCandidate.GetGenericArguments().Length] : null;

            // Determining whether we can use this method candidate with current parameter types.
            for (int idx = 0; idx < parameters.Length; idx++)
            {
                TypeInfo parameterType = parameters[idx].ParameterType.GetTypeInfo();
                TypeInfo actualType = methodLookupKey.ParameterTypes[idx].GetTypeInfo();

                if (TypesMatchRecursive(parameterType, actualType, genericArguments))
                {
                    continue;
                }
                
                parameterTypesMatched = false;
                break;
            }

            if (!parameterTypesMatched)
            {
                continue;
            }
            
            if (genericArguments == null)
            {
                // Return first found method candidate with matching parameters.
                return methodCandidate;
            }

            bool genericArgumentsResolved = true;

            foreach (Type genericArgument in genericArguments)
            {
                if (genericArgument == null)
                {
                    genericArgumentsResolved = false;
                }
            }

            if (genericArgumentsResolved)
            {
                return methodCandidate.MakeGenericMethod(genericArguments);
            }
        }

        return null;
    }

    private static bool TypesMatchRecursive(TypeInfo? parameterType, TypeInfo? actualType, IList<Type?>? genericArguments)
    {
        if (parameterType == null || actualType == null)
        {
            return false;
        }

        if (parameterType.IsGenericParameter)
        {
            if (genericArguments == null)
            {
                return false;
            }

            int position = parameterType.GenericParameterPosition;

            // Return false if this generic parameter has been identified, and it's not the same as actual type
            if (genericArguments[position] != null && genericArguments[position]?.GetTypeInfo() != actualType)
            {
                return false;
            }

            genericArguments[position] = actualType.AsType();

            return true;
        }

        if (!parameterType.ContainsGenericParameters)
        {
            return parameterType != typeof(object).GetTypeInfo()
                ? parameterType.IsAssignableFrom(actualType)
                : parameterType == actualType;
        }

        if (parameterType.IsArray)
        {
            // Return false if parameterType is array whereas actualType isn't
            if (!actualType.IsArray)
            {
                return false;
            }

            Type? parameterElementType = parameterType.GetElementType();
            Type? actualElementType = actualType.GetElementType();

            return TypesMatchRecursive(parameterElementType?.GetTypeInfo(), actualElementType?.GetTypeInfo(), genericArguments);
        }

        if (!actualType.IsGenericType || parameterType.GetGenericTypeDefinition() != actualType.GetGenericTypeDefinition())
        {
            return false;
        }

        for (int idx = 0; idx < parameterType.GenericTypeArguments.Length; idx++)
        {
            Type parameterGenericArgument = parameterType.GenericTypeArguments[idx];
            Type actualGenericArgument = actualType.GenericTypeArguments[idx];

            if (!TypesMatchRecursive(parameterGenericArgument.GetTypeInfo(), actualGenericArgument.GetTypeInfo(), genericArguments))
            {
                return false;
            }
        }

        return true;
    }
}