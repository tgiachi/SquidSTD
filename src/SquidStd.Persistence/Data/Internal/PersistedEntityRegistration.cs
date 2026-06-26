using DryIoc;
using SquidStd.Persistence.Abstractions.Interfaces.Persistence;

namespace SquidStd.Persistence.Data.Internal;

/// <summary>
/// A declarative persisted-entity registration consumed at bootstrap. The <see cref="Register" /> closure
/// captures the entity and key types at registration time, so registry population needs no reflection.
/// </summary>
public sealed record PersistedEntityRegistration(
    ushort TypeId,
    string TypeName,
    Action<IPersistenceEntityRegistry, IResolverContext> Register
);
