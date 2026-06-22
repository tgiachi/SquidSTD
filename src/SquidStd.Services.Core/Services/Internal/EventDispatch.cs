namespace SquidStd.Services.Core.Services.Internal;

/// <summary>
/// A queued event dispatch with a completion signal, executed by the event bus dispatcher loop.
/// </summary>
internal sealed class EventDispatch
{
    private readonly CancellationToken _cancellationToken;
    private readonly Func<Task> _dispatch;
    private readonly TaskCompletionSource _completion;

    public Task Completion => _completion.Task;

    public EventDispatch(Func<Task> dispatch, CancellationToken cancellationToken)
    {
        _dispatch = dispatch;
        _cancellationToken = cancellationToken;
        _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public async Task ExecuteAsync()
    {
        try
        {
            await _dispatch();
            _completion.TrySetResult();
        }
        catch (OperationCanceledException)
        {
            _completion.TrySetCanceled(_cancellationToken);
        }
        catch (Exception exception)
        {
            _completion.TrySetException(exception);
        }
    }
}
