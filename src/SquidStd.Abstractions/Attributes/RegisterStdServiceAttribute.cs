namespace SquidStd.Abstractions.Attributes;

/// <summary>
///     Marks a service implementation for generated SquidStd lifecycle registration.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RegisterStdServiceAttribute : Attribute
{
    /// <summary>
    ///     Gets the service contract registered for the annotated implementation.
    /// </summary>
    public Type? ServiceType { get; }

    /// <summary>
    ///     Gets or sets the lifecycle start priority.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    ///     Initializes a new instance of the attribute.
    /// </summary>
    /// <param name="serviceType">The service contract registered for the annotated implementation.</param>
    public RegisterStdServiceAttribute(Type? serviceType = null)
    {
        ServiceType = serviceType;
    }
}
