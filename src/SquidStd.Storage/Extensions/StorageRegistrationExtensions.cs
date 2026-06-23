using DryIoc;
using SquidStd.Storage.Abstractions.Data.Config;
using SquidStd.Storage.Abstractions.Interfaces;
using SquidStd.Storage.Services;

namespace SquidStd.Storage.Extensions;

/// <summary>
/// DryIoc registration helpers for the local file storage provider.
/// </summary>
public static class StorageRegistrationExtensions
{
    /// <summary>Registers file-backed <see cref="IStorageService" /> and YAML-backed <see cref="IObjectStorageService" />.</summary>
    public static IContainer AddFileStorage(this IContainer container, StorageConfig? config = null)
    {
        ArgumentNullException.ThrowIfNull(container);

        container.RegisterInstance(config ?? new StorageConfig());
        container.Register<IStorageService, FileStorageService>(Reuse.Singleton);
        container.Register<IObjectStorageService, YamlObjectStorageService>(Reuse.Singleton);

        return container;
    }
}
