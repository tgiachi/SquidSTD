namespace SquidStd.Abstractions.Attributes;

/// <summary>
///     Marks a configuration type for generated config-section registration.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RegisterConfigSectionAttribute : Attribute
{
    /// <summary>
    ///     Gets the configuration section name.
    /// </summary>
    public string? SectionName { get; }

    /// <summary>
    ///     Gets or sets the config loading priority.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    ///     Initializes a new instance of the attribute.
    /// </summary>
    /// <param name="sectionName">The configuration section name.</param>
    public RegisterConfigSectionAttribute(string? sectionName = null)
    {
        SectionName = sectionName;
    }
}
