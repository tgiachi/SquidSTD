using System.ComponentModel;
using SquidStd.Tui.Internal;

namespace SquidStd.Tui.Binding;

/// <summary>
/// Wires ViewModel (<see cref="INotifyPropertyChanged" />) properties and commands to view targets, and
/// owns the lifetime of every subscription it creates. Dispose to release them all.
/// </summary>
public sealed partial class ViewBinder : IDisposable
{
    private readonly List<IDisposable> _subscriptions = new();
    private readonly Action<Action> _marshal;

    /// <param name="marshal">
    /// Runs an update on the UI thread. Defaults to running inline; the host passes a delegate over
    /// <c>Application.Invoke</c> so updates raised from background threads reach the Terminal.Gui loop.
    /// </param>
    public ViewBinder(Action<Action>? marshal = null)
    {
        _marshal = marshal ?? (action => action());
    }

    /// <summary>Applies <paramref name="apply" /> now and whenever <paramref name="propertyName" /> changes.</summary>
    public void OneWay(INotifyPropertyChanged source, string propertyName, Action apply)
    {
        apply();

        void Handler(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is null || string.Equals(e.PropertyName, propertyName, StringComparison.Ordinal))
            {
                _marshal(apply);
            }
        }

        source.PropertyChanged += Handler;
        _subscriptions.Add(new Unsubscriber(() => source.PropertyChanged -= Handler));
    }

    /// <summary>
    /// Binds a source property both ways. <paramref name="applyToTarget" /> runs on source changes;
    /// <paramref name="writeToSource" /> runs when the target raises a change. A reentrancy guard stops
    /// the source→target→source feedback loop.
    /// </summary>
    public void TwoWay(
        INotifyPropertyChanged source,
        string propertyName,
        Action applyToTarget,
        Action<Action> subscribeTargetChanged,
        Action writeToSource
    )
    {
        var guard = new ReentryGuard();

        applyToTarget();

        void SourceHandler(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is not null && !string.Equals(e.PropertyName, propertyName, StringComparison.Ordinal))
            {
                return;
            }

            if (guard.IsBusy)
            {
                return;
            }

            _marshal(() =>
            {
                using (guard.Enter())
                {
                    applyToTarget();
                }
            });
        }

        source.PropertyChanged += SourceHandler;
        _subscriptions.Add(new Unsubscriber(() => source.PropertyChanged -= SourceHandler));

        subscribeTargetChanged(() =>
        {
            if (guard.IsBusy)
            {
                return;
            }

            using (guard.Enter())
            {
                writeToSource();
            }
        });
    }

    private void Track(IDisposable subscription)
    {
        _subscriptions.Add(subscription);
    }

    public void Dispose()
    {
        for (var i = 0; i < _subscriptions.Count; i++)
        {
            _subscriptions[i].Dispose();
        }

        _subscriptions.Clear();
    }
}
