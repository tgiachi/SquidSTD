using System.Threading.Channels;
using Serilog;
using SquidStd.Abstractions.Interfaces.Services;
using SquidStd.Core.Data.Jobs;
using SquidStd.Core.Interfaces.Jobs;
using SquidStd.Services.Core.Services.Internal;

namespace SquidStd.Services.Core.Services;

/// <summary>
///     Schedules jobs on a fixed set of worker threads.
/// </summary>
public sealed class JobSystemService : IJobSystem, ISquidStdService
{
    private readonly Channel<JobItem> _channel;
    private readonly JobsConfig _config;
    private readonly ILogger _logger = Log.ForContext<JobSystemService>();
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly Thread[] _workers;
    private int _activeCount;
    private long _completedCount;
    private int _disposed;
    private int _pendingCount;
    private int _started;

    /// <inheritdoc />
    public int ActiveCount => Volatile.Read(ref _activeCount);

    /// <inheritdoc />
    public long CompletedCount => Interlocked.Read(ref _completedCount);

    /// <inheritdoc />
    public int PendingCount => Volatile.Read(ref _pendingCount);

    /// <inheritdoc />
    public int WorkerCount { get; }

    /// <summary>
    ///     Initializes the job system service.
    /// </summary>
    /// <param name="config">Job system configuration.</param>
    public JobSystemService(JobsConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
        WorkerCount = ResolveWorkerCount(config.WorkerThreadCount);
        _channel = Channel.CreateUnbounded<JobItem>(
            new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            }
        );
        _workers = new Thread[WorkerCount];
    }

    /// <inheritdoc />
    public Task ScheduleAsync(Action work, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);
        ThrowIfDisposed();
        ThrowIfNotStarted();

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var item = new JobItem(
            () =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetCanceled(cancellationToken);

                    return;
                }

                try
                {
                    work();
                    completion.TrySetResult();
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
                {
                    completion.TrySetCanceled(ex.CancellationToken);
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
            },
            () => completion.TrySetCanceled(_shutdownCts.Token)
        );

        Enqueue(item);

        return completion.Task;
    }

    /// <inheritdoc />
    public Task<T> ScheduleAsync<T>(Func<T> work, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);
        ThrowIfDisposed();
        ThrowIfNotStarted();

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<T>(cancellationToken);
        }

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var item = new JobItem(
            () =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetCanceled(cancellationToken);

                    return;
                }

                try
                {
                    completion.TrySetResult(work());
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
                {
                    completion.TrySetCanceled(ex.CancellationToken);
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
            },
            () => completion.TrySetCanceled(_shutdownCts.Token)
        );

        Enqueue(item);

        return completion.Task;
    }

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
        {
            return ValueTask.CompletedTask;
        }

        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        for (var i = 0; i < _workers.Length; i++)
        {
            var index = i;
            var worker = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = $"SquidStd.JobWorker.{index}"
            };
            _workers[index] = worker;
            worker.Start();
        }

        _logger.Information("JobSystemService started with {WorkerCount} worker thread(s)", WorkerCount);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        Stop(cancellationToken);

        return ValueTask.CompletedTask;
    }

    private void Enqueue(JobItem item)
    {
        Interlocked.Increment(ref _pendingCount);

        if (!_channel.Writer.TryWrite(item))
        {
            Interlocked.Decrement(ref _pendingCount);

            throw new ObjectDisposedException(nameof(JobSystemService));
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        _logger.Error(args.Exception, "Unobserved task exception");
        args.SetObserved();
    }

    private static int ResolveWorkerCount(int configured)
    {
        return configured > 0 ? configured : Math.Max(1, Environment.ProcessorCount - 1);
    }

    private void Stop(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
        {
            return;
        }

        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        _channel.Writer.TryComplete();
        _shutdownCts.Cancel();

        while (_channel.Reader.TryRead(out var item))
        {
            Interlocked.Decrement(ref _pendingCount);
            item.Cancel();
        }

        var timeoutMs = Math.Max(1, (int)(_config.ShutdownTimeoutSeconds * 1000.0));
        var deadline = Environment.TickCount64 + timeoutMs;
        var stillActive = 0;

        for (var i = 0; i < _workers.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var worker = _workers[i];

            if (worker is null)
            {
                continue;
            }

            var remaining = (int)Math.Max(0, deadline - Environment.TickCount64);

            if (!worker.Join(remaining))
            {
                stillActive++;
            }
        }

        if (stillActive > 0)
        {
            _logger.Warning("JobSystemService shutdown timed out, {Count} worker(s) still active", stillActive);
        }

        _shutdownCts.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(JobSystemService));
        }
    }

    private void ThrowIfNotStarted()
    {
        if (Volatile.Read(ref _started) == 0)
        {
            throw new InvalidOperationException("JobSystemService must be started before scheduling jobs.");
        }
    }

    private void WorkerLoop()
    {
        var cancellationToken = _shutdownCts.Token;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                JobItem item;

                try
                {
                    item = _channel.Reader.ReadAsync(cancellationToken).AsTask().GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ChannelClosedException)
                {
                    break;
                }

                Interlocked.Decrement(ref _pendingCount);
                Interlocked.Increment(ref _activeCount);

                try
                {
                    item.Run();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "JobSystemService worker caught an exception outside the job wrapper");
                }
                finally
                {
                    Interlocked.Decrement(ref _activeCount);
                    Interlocked.Increment(ref _completedCount);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "JobSystemService worker loop terminated unexpectedly");
        }
    }

    /// <summary>
    ///     Releases worker resources.
    /// </summary>
    public void Dispose()
    {
        Stop(CancellationToken.None);
    }
}
