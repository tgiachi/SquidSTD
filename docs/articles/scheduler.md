# Scheduler (Cron)

`ICronScheduler` (in `SquidStd.Core`, implemented in `SquidStd.Services.Core`) runs asynchronous jobs on
standard 5-field cron expressions evaluated in UTC.

- `Schedule(name, cronExpression, handler)` → returns a job id.
- `Unschedule(jobId)` / `UnscheduleByName(name)`.
- `Jobs` — a snapshot of registered jobs (`CronJobInfo`).

Each job is a one-shot, self-rescheduling timer on the timer wheel: when it fires, the handler is
dispatched through `IJobSystem`, and the next occurrence is registered. An occurrence is **skipped** if the
previous run of the same job is still in flight. Because the timer wheel must be advanced, the package also
provides `TimerWheelPumpService`, which pumps the wheel on a background loop.

Register everything (after `RegisterCoreServices`) with `RegisterSchedulerServices()`:

```csharp
using DryIoc;
using SquidStd.Core.Interfaces.Scheduling;
using SquidStd.Services.Core.Extensions;

container.RegisterSchedulerServices();

var scheduler = container.Resolve<ICronScheduler>();
scheduler.Schedule("cleanup", "0 3 * * *", async ct =>
{
    await DoCleanupAsync(ct);
});
```
