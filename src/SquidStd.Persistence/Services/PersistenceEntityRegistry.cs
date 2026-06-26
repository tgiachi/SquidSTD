using SquidStd.Persistence.Abstractions.Interfaces.Persistence;

namespace SquidStd.Persistence.Services;

/// <summary>Default registry of persisted entity descriptors keyed by type id and entity/key pair.</summary>
public sealed class PersistenceEntityRegistry : IPersistenceEntityRegistry
{
    private readonly Dictionary<ushort, IPersistenceEntityDescriptor> _byTypeId = [];
    private readonly Dictionary<(Type Entity, Type Key), IPersistenceEntityDescriptor> _byTypePair = [];
    private readonly Lock _sync = new();
    private bool _frozen;

    public bool IsFrozen
    {
        get
        {
            lock (_sync)
            {
                return _frozen;
            }
        }
    }

    public void Freeze()
    {
        lock (_sync)
        {
            _frozen = true;
        }
    }

    public void Register<TEntity, TKey>(IPersistenceEntityDescriptor<TEntity, TKey> descriptor)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        lock (_sync)
        {
            if (_frozen)
            {
                throw new InvalidOperationException("Cannot register entities after the registry is frozen.");
            }

            if (!_byTypeId.TryAdd(descriptor.TypeId, descriptor))
            {
                throw new InvalidOperationException($"Type id {descriptor.TypeId} is already registered.");
            }

            _byTypePair[(typeof(TEntity), typeof(TKey))] = descriptor;
        }
    }

    public IPersistenceEntityDescriptor GetDescriptor(ushort typeId)
    {
        lock (_sync)
        {
            if (_byTypeId.TryGetValue(typeId, out var descriptor))
            {
                return descriptor;
            }
        }

        throw new InvalidOperationException($"No persisted entity registered for type id {typeId}.");
    }

    public IPersistenceEntityDescriptor<TEntity, TKey> GetDescriptor<TEntity, TKey>()
        where TKey : notnull
    {
        lock (_sync)
        {
            if (_byTypePair.TryGetValue((typeof(TEntity), typeof(TKey)), out var descriptor))
            {
                return (IPersistenceEntityDescriptor<TEntity, TKey>)descriptor;
            }
        }

        throw new InvalidOperationException($"No persisted entity registered for {typeof(TEntity).Name}/{typeof(TKey).Name}.");
    }

    public IReadOnlyCollection<IPersistenceEntityDescriptor> GetRegisteredDescriptors()
    {
        lock (_sync)
        {
            return [.. _byTypeId.Values];
        }
    }

    public bool IsRegistered(ushort typeId)
    {
        lock (_sync)
        {
            return _byTypeId.ContainsKey(typeId);
        }
    }

    public bool IsRegistered<TEntity, TKey>()
        where TKey : notnull
    {
        lock (_sync)
        {
            return _byTypePair.ContainsKey((typeof(TEntity), typeof(TKey)));
        }
    }
}
