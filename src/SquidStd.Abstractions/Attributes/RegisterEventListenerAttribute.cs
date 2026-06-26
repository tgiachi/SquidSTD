namespace SquidStd.Abstractions.Attributes;

/// <summary>
/// Marks an event listener for generated registration.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RegisterEventListenerAttribute : Attribute { }
