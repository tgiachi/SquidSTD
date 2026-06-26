using DryIoc;
using SquidStd.Abstractions.Extensions.Container;
using SquidStd.Core.Interfaces.Serialization;
using SquidStd.Persistence.Abstractions.Interfaces.Persistence;
using SquidStd.Persistence.Data;
using SquidStd.Persistence.Data.Internal;

namespace SquidStd.Persistence.Extensions;

/// <summary>Registers persisted entity types for descriptor construction at bootstrap.</summary>
public static class PersistenceRegistrationExtensions
{
    /// <summary>Records a persisted entity type; its descriptor is built and registered at bootstrap.</summary>
    public static IContainer RegisterPersistedEntity<TEntity, TKey>(
        this IContainer container,
        ushort typeId,
        string typeName,
        int schemaVersion,
        Func<TEntity, TKey> keySelector
    )
        where TKey : notnull
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        ArgumentNullException.ThrowIfNull(keySelector);

        container.AddToRegisterTypedList(
            new PersistedEntityRegistration(
                typeId,
                typeName,
                (registry, resolver) => registry.Register(
                    new PersistenceEntityDescriptor<TEntity, TKey>(
                        resolver.Resolve<IDataSerializer>(),
                        resolver.Resolve<IDataDeserializer>(),
                        typeId,
                        typeName,
                        schemaVersion,
                        keySelector
                    )
                )
            )
        );

        return container;
    }

    /// <summary>Builds and registers all recorded persisted-entity descriptors into the registry.</summary>
    public static IContainer ApplyPersistedEntityRegistrations(this IContainer container)
    {
        if (!container.IsRegistered<List<PersistedEntityRegistration>>())
        {
            return container;
        }

        var registry = container.Resolve<IPersistenceEntityRegistry>();
        var registrations = container.Resolve<List<PersistedEntityRegistration>>();

        for (var i = 0; i < registrations.Count; i++)
        {
            registrations[i].Register(registry, container);
        }

        return container;
    }
}
