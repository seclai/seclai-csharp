#if !NET9_0_OR_GREATER
namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill of the .NET 9 attribute. C# 13 and later compilers read it by name from
/// referenced assemblies, so it steers a consumer's overload resolution even though
/// this library targets netstandard2.0.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class OverloadResolutionPriorityAttribute : Attribute
{
    public OverloadResolutionPriorityAttribute(int priority) => Priority = priority;

    public int Priority { get; }
}
#endif
