namespace SquidStd.Core.Data.Timing;

/// <summary>
/// Configuration for the timer wheel pump service.
/// </summary>
public sealed class TimerWheelPumpConfig
{
    /// <summary>
    /// Gets or sets how often the pump advances the timer wheel.
    /// </summary>
    public TimeSpan PumpInterval { get; set; } = TimeSpan.FromMilliseconds(250);
}
