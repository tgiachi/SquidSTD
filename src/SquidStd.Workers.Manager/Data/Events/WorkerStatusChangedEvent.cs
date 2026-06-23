using SquidStd.Core.Interfaces.Events;
using SquidStd.Workers.Abstractions.Types;

namespace SquidStd.Workers.Manager.Data.Events;

/// <summary>
/// Published on the event bus when a worker's status changes: discovered (<paramref name="OldStatus" /> null),
/// gone Offline (via the sweep), or returned (Offline → Idle/Busy).
/// </summary>
/// <param name="WorkerId">The worker whose status changed.</param>
/// <param name="OldStatus">Previous status, or <c>null</c> when the worker was just discovered.</param>
/// <param name="NewStatus">The new status.</param>
public sealed record WorkerStatusChangedEvent(string WorkerId, WorkerStatusType? OldStatus, WorkerStatusType NewStatus) : IEvent;
