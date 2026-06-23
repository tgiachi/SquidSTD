using DryIoc;
using SquidStd.Storage.Abstractions.Interfaces;
using SquidStd.Storage.S3.Data.Config;
using SquidStd.Storage.S3.Services;

namespace SquidStd.Storage.S3.Extensions;

/// <summary>
/// DryIoc registration helpers for the S3-compatible (MinIO) storage provider.
/// </summary>
public static class S3StorageRegistrationExtensions
{
    /// <summary>Registers <see cref="IStorageService" /> backed by S3/MinIO.</summary>
    public static IContainer AddS3Storage(this IContainer container, S3StorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(options);

        container.RegisterInstance(options);
        container.Register<IStorageService, S3StorageService>(Reuse.Singleton);

        return container;
    }
}
