using Bacon.Scheduler.Extensions;
using Bacon.Scheduler.Tests.Fakes;
using System.Reflection;

namespace Bacon.Scheduler.Tests.Extensions;

[TestFixture]
public class TypeExtensionsTests
{
    [Test]
    public void GetNonOpenMatchingMethod_InterfaceTypeParameterlessMethod_ResolvesMethod()
    {
        MethodInfo? method = typeof(ITestJob).GetNonOpenMatchingMethod(nameof(ITestJob.RunAsync), Type.EmptyTypes);

        Assert.That(method?.Name, Is.EqualTo(nameof(ITestJob.RunAsync)));
    }

    [Test]
    public void GetNonOpenMatchingMethod_ConcreteTypeWithParameters_ResolvesOverload()
    {
        MethodInfo? method = typeof(TestJob).GetNonOpenMatchingMethod(nameof(TestJob.RunWithArgsAsync), [typeof(int), typeof(string)]);

        Assert.Multiple(() =>
        {
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.GetParameters().Select(p => p.ParameterType), Is.EqualTo(new[] { typeof(int), typeof(string) }));
        });
    }

    [Test]
    public void GetNonOpenMatchingMethod_WrongParameterCount_ReturnsNull()
    {
        MethodInfo? method = typeof(TestJob).GetNonOpenMatchingMethod(nameof(TestJob.RunWithArgsAsync), [typeof(int)]);

        Assert.That(method, Is.Null);
    }

    [Test]
    public void GetNonOpenMatchingMethod_WrongParameterTypes_ReturnsNull()
    {
        MethodInfo? method = typeof(TestJob).GetNonOpenMatchingMethod(nameof(TestJob.RunWithArgsAsync), [typeof(int), typeof(int)]);

        Assert.That(method, Is.Null);
    }

    [Test]
    public void GetNonOpenMatchingMethod_UnknownMethodName_ReturnsNull()
    {
        MethodInfo? method = typeof(TestJob).GetNonOpenMatchingMethod("DoesNotExist", Type.EmptyTypes);

        Assert.That(method, Is.Null);
    }

    [Test]
    public void GetNonOpenMatchingMethod_ExplicitInterfaceImplementation_ResolvesByNormalizedName()
    {
        MethodInfo? method = typeof(ExplicitTestJob).GetNonOpenMatchingMethod(nameof(IExplicitTestJob.Run), Type.EmptyTypes);

        Assert.That(method, Is.Not.Null);
    }

    [Test]
    public void GetNonOpenMatchingMethod_ResultIsCached_ReturnsSameMethodInfoOnSecondLookup()
    {
        MethodInfo? first = typeof(TestJob).GetNonOpenMatchingMethod(nameof(TestJob.RunAsync), Type.EmptyTypes);
        MethodInfo? second = typeof(TestJob).GetNonOpenMatchingMethod(nameof(TestJob.RunAsync), Type.EmptyTypes);

        Assert.That(first, Is.SameAs(second));
    }
}
