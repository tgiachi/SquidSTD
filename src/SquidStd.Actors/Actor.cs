using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using Serilog;
using SquidStd.Actors.Data;
using SquidStd.Actors.Interfaces;
using SquidStd.Actors.Types;
using ILogger = Serilog.ILogger;

namespace SquidStd.Actors;

/// <summary>
///     Base class for an actor: a single-consumer mailbox that processes messages in FIFO order on one
///     logical thread, so handler state is mutated without locks. Send fire-and-forget messages with
///     <see cref="TellAsync" /> and request/response messages with <see cref="AskAsync{TRequest,TReply}" />.
/// </summary>
/// <typeparam name="TMessage">The base type of every message this actor accepts.</typeparam>
public abstract class Actor<TMessage> : IAsyncDisposable
{
    private readonly ActorOptions _options;
    private readonly ActionBlock<TMessage> _mailbox;
    private readonly CancellationTokenSource _shutdown;
    private readonly ConcurrentDictionary<IActorRequestCore, byte> _outstanding;
    private readonly ILogger _logger;
    private int _disposed;

    /// <summary>Number of messages waiting in the mailbox.</summary>
    public int PendingCount
    {
        get { return _mailbox.InputCount; }
    }

    /// <summary>Initializes the actor and starts its mailbox consumer.</summary>
    /// <param name="options">Mailbox options; defaults are used when null.</param>
    protected Actor(ActorOptions? options = null)
    {
        _options = options ?? new ActorOptions();
        _shutdown = new CancellationTokenSource();
        _outstanding = new ConcurrentDictionary<IActorRequestCore, byte>();
        _logger = Log.ForContext(GetType());

        var blockOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 1,
            EnsureOrdered = true,
            BoundedCapacity = _options.OverflowPolicy == ActorOverflowPolicy.Unbounded
                ? DataflowBlockOptions.Unbounded
                : _options.Capacity,
            CancellationToken = _shutdown.Token
        };

        _mailbox = new ActionBlock<TMessage>(ProcessAsync, blockOptions);
    }

    /// <summary>Enqueues a fire-and-forget message. Returns false only when dropped (DropNewest).</summary>
    /// <param name="message">The message to enqueue.</param>
    /// <param name="cancellationToken">Cancels the enqueue wait under the Wait policy.</param>
    /// <returns>True when accepted; false when dropped.</returns>
    public async ValueTask<bool> TellAsync(TMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ThrowIfDisposed();

        if (_options.OverflowPolicy == ActorOverflowPolicy.DropNewest)
        {
            return _mailbox.Post(message);
        }

        return await _mailbox.SendAsync(message, cancellationToken);
    }

    /// <summary>Enqueues a request and awaits its typed reply.</summary>
    /// <param name="request">The request message; it is also a <typeparamref name="TMessage" />.</param>
    /// <param name="cancellationToken">Acts as a timeout for the reply.</param>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TReply">The reply type.</typeparam>
    /// <returns>The reply.</returns>
    public async Task<TReply> AskAsync<TRequest, TReply>(
        TRequest request, CancellationToken cancellationToken = default
    )
        where TRequest : TMessage, IActorRequest<TReply>
    {
        ArgumentNullException.ThrowIfNull(request);
        ThrowIfDisposed();

        _outstanding[request] = 0;

        using var registration =
            cancellationToken.Register(() => request.Fail(new OperationCanceledException(cancellationToken))
            );

        try
        {
            var accepted = await TellAsync(request, cancellationToken);

            if (!accepted)
            {
                request.Fail(new InvalidOperationException("Actor mailbox is full; the request was dropped."));
            }

            return await request.Completion;
        }
        finally
        {
            _outstanding.TryRemove(request, out _);
        }
    }

    /// <summary>Handles a single message. Runs on the mailbox consumer; mutate actor state here freely.</summary>
    /// <param name="message">The message to handle.</param>
    /// <param name="cancellationToken">Cancelled when the actor is disposed.</param>
    protected abstract ValueTask ReceiveAsync(TMessage message, CancellationToken cancellationToken);

    /// <summary>Optional hook invoked after an isolated failure. Default is a no-op.</summary>
    /// <param name="message">The message whose handler threw.</param>
    /// <param name="error">The thrown exception.</param>
    protected virtual ValueTask OnErrorAsync(TMessage message, Exception error)
    {
        return ValueTask.CompletedTask;
    }

    private async Task ProcessAsync(TMessage message)
    {
        try
        {
            await ReceiveAsync(message, _shutdown.Token);
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            if (message is IActorRequestCore cancelledRequest)
            {
                cancelledRequest.Fail(new OperationCanceledException(_shutdown.Token));
            }
        }
        catch (Exception ex)
        {
            if (message is IActorRequestCore request)
            {
                request.Fail(ex);
            }

            _logger.Error(ex, "Actor {ActorType} failed handling {MessageType}", GetType().Name, message?.GetType().Name);

            try
            {
                await OnErrorAsync(message, ex);
            }
            catch (Exception hookError)
            {
                _logger.Error(hookError, "Actor {ActorType} OnError hook threw", GetType().Name);
            }

            if (_options.ErrorPolicy == ActorErrorPolicy.StopOnError)
            {
                foreach (var pending in _outstanding.Keys)
                {
                    pending.Fail(new InvalidOperationException("Actor stopped due to a handler failure.", ex));
                }

                throw;
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    /// <summary>Completes the mailbox, drains in-flight work, and faults any still-pending requests.</summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _shutdown.Cancel();
        _mailbox.Complete();

        try
        {
            await _mailbox.Completion;
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "Actor {ActorType} mailbox completed with fault during dispose", GetType().Name);
        }

        foreach (var request in _outstanding.Keys)
        {
            request.Fail(new ObjectDisposedException(GetType().Name));
        }

        _outstanding.Clear();
        _shutdown.Dispose();
        GC.SuppressFinalize(this);
    }
}
