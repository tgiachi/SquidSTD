namespace SquidStd.Tui.Internal;

/// <summary>Single-threaded reentrancy flag used to stop two-way binding write-back loops.</summary>
internal sealed class ReentryGuard
{
    private bool _busy;

    public bool IsBusy
    {
        get { return _busy; }
    }

    public IDisposable Enter()
    {
        _busy = true;

        return new Scope(this);
    }

    private sealed class Scope : IDisposable
    {
        private readonly ReentryGuard _owner;

        public Scope(ReentryGuard owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            _owner._busy = false;
        }
    }
}
