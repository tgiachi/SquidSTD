namespace SquidStd.Tui.Types.Tui;

/// <summary>Binding direction for a DSL node that supports both.</summary>
public enum BindMode
{
    /// <summary>Source-to-target only.</summary>
    OneWay,

    /// <summary>Source-to-target and target-to-source.</summary>
    TwoWay
}
