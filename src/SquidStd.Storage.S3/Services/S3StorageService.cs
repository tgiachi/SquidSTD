using System.Runtime.CompilerServices;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using SquidStd.Storage.Abstractions.Interfaces;
using SquidStd.Storage.S3.Data.Config;

namespace SquidStd.Storage.S3.Services;

/// <summary>
/// S3-compatible <see cref="IStorageService" /> backed by the MinIO client. The bucket is created
/// lazily on first use.
/// </summary>
public sealed class S3StorageService : IStorageService, IDisposable
{
    private readonly IMinioClient _client;
    private readonly string _bucket;
    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private bool _bucketReady;
    private int _disposed;

    public S3StorageService(S3StorageOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.AccessKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.SecretKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Bucket);

        var builder = new MinioClient()
            .WithEndpoint(options.Endpoint)
            .WithCredentials(options.AccessKey, options.SecretKey)
            .WithSSL(options.UseSsl);

        if (!string.IsNullOrWhiteSpace(options.Region))
        {
            builder = builder.WithRegion(options.Region);
        }

        _client = builder.Build();
        _bucket = options.Bucket;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(string key, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);

        var bytes = data.ToArray();
        using var stream = new MemoryStream(bytes, writable: false);

        await _client.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(_bucket)
                .WithObject(key)
                .WithStreamData(stream)
                .WithObjectSize(bytes.LongLength),
            cancellationToken
        );
    }

    /// <inheritdoc />
    public async ValueTask<byte[]?> LoadAsync(string key, CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);

        using var buffer = new MemoryStream();

        try
        {
            await _client.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(_bucket)
                    .WithObject(key)
                    .WithCallbackStream(async (stream, ct) => await stream.CopyToAsync(buffer, ct)),
                cancellationToken
            );
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }

        return buffer.ToArray();
    }

    /// <inheritdoc />
    public async ValueTask<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);

        try
        {
            await _client.StatObjectAsync(
                new StatObjectArgs().WithBucket(_bucket).WithObject(key),
                cancellationToken
            );

            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async ValueTask<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!await ExistsAsync(key, cancellationToken))
        {
            return false;
        }

        await _client.RemoveObjectAsync(
            new RemoveObjectArgs().WithBucket(_bucket).WithObject(key),
            cancellationToken
        );

        return true;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ListKeysAsync(
        string? prefix = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await EnsureBucketAsync(cancellationToken);

        var args = new ListObjectsArgs().WithBucket(_bucket).WithRecursive(true);

        if (!string.IsNullOrEmpty(prefix))
        {
            args = args.WithPrefix(prefix);
        }

        await foreach (var item in _client.ListObjectsEnumAsync(args, cancellationToken))
        {
            if (!item.IsDir)
            {
                yield return item.Key;
            }
        }
    }

    private async ValueTask EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (_bucketReady)
        {
            return;
        }

        await _bucketLock.WaitAsync(cancellationToken);

        try
        {
            if (_bucketReady)
            {
                return;
            }

            var exists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucket), cancellationToken);

            if (!exists)
            {
                await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucket), cancellationToken);
            }

            _bucketReady = true;
        }
        finally
        {
            _bucketLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _client.Dispose();
        _bucketLock.Dispose();
    }
}
