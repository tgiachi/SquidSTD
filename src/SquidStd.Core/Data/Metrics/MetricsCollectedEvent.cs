using SquidStd.Core.Interfaces.Events;

namespace SquidStd.Core.Data.Metrics;

/// <summary>
/// Event published after a metrics snapshot has been collected.
/// </summary>
/// <param name="Snapshot">The collected metrics snapshot.</param>
public sealed record MetricsCollectedEvent(MetricsSnapshot Snapshot) : IEvent;
