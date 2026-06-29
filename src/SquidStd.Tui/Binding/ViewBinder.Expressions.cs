using System.ComponentModel;
using System.Linq.Expressions;
using SquidStd.Tui.Internal;

namespace SquidStd.Tui.Binding;

public sealed partial class ViewBinder
{
    /// <summary>One-way bind a source property to a target member, by expression.</summary>
    public void OneWay<TSource, TTarget, TValue>(
        TSource source,
        Expression<Func<TSource, TValue>> sourceProperty,
        TTarget target,
        Expression<Func<TTarget, TValue>> targetProperty
    )
        where TSource : INotifyPropertyChanged
    {
        var name = PropertyPath.NameOf(sourceProperty);
        var read = sourceProperty.Compile();
        var write = PropertyPath.Setter(targetProperty);

        OneWay(source, name, () => write(target, read(source)));
    }

    /// <summary>Two-way bind a source property to a target member, by expression, given the target's change event.</summary>
    public void TwoWay<TSource, TTarget, TValue>(
        TSource source,
        Expression<Func<TSource, TValue>> sourceProperty,
        TTarget target,
        Expression<Func<TTarget, TValue>> targetProperty,
        Action<Action> subscribeTargetChanged
    )
        where TSource : INotifyPropertyChanged
    {
        var name = PropertyPath.NameOf(sourceProperty);
        var read = sourceProperty.Compile();
        var readTarget = targetProperty.Compile();
        var write = PropertyPath.Setter(targetProperty);
        var writeSource = PropertyPath.Setter(sourceProperty);

        TwoWay(
            source,
            name,
            () => write(target, read(source)),
            subscribeTargetChanged,
            () => writeSource(source, readTarget(target))
        );
    }
}
