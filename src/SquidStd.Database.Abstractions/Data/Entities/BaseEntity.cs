namespace SquidStd.Database.Abstractions.Data.Entities;

/// <summary>
/// Base class for all persisted entities: a Guid identity plus UTC create/update timestamps.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Gets or sets the primary key (assigned on insert when empty).</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the UTC creation timestamp (set on insert).</summary>
    public DateTimeOffset Created { get; set; }

    /// <summary>Gets or sets the UTC last-update timestamp (set on insert and update).</summary>
    public DateTimeOffset Updated { get; set; }
}
