# Build a worker system

Run a background job system end to end: register workers, enqueue a job through the manager, and handle it.

## What you'll build

A console host that wires in-memory messaging, the worker runtime (`SquidStd.Workers`), and the worker manager
(`SquidStd.Workers.Manager`) on top of `SquidStdBootstrap`. It enqueues a `greet` job and a handler picks it up
and prints a greeting.

## Prerequisites

- .NET 10 SDK
- `dotnet add package SquidStd.Workers`
- `dotnet add package SquidStd.Workers.Manager`
- `dotnet add package SquidStd.Messaging`
- `dotnet add package SquidStd.Services.Core`

## Steps

### 1. Register messaging, workers and the manager

`ConfigureServices` runs against the bootstrap's DryIoc container. Add the in-memory queue, the worker runtime, a
job handler, and the manager (which provides the job scheduler).

[!code-csharp[](../../samples/SquidStd.Samples.WorkerSystem/Program.cs#step-1)]

### 2. Implement the job handler

A handler implements `IJobHandler`: it declares the `JobName` it processes and does the work in `HandleAsync`,
reading any string parameters off the `JobRequest`.

[!code-csharp[](../../samples/SquidStd.Samples.WorkerSystem/Program.cs#step-2)]

### 3. Start, enqueue, and stop

Start the host, resolve the `IJobScheduler`, enqueue a `greet` job with a parameter, give the worker a moment to
consume it, then stop cleanly.

[!code-csharp[](../../samples/SquidStd.Samples.WorkerSystem/Program.cs#step-3)]

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.WorkerSystem
```

You'll see `Hello, squid! (job: greet)` once the worker consumes the enqueued job.

When hosting inside an ASP.NET Core app, you can instead expose the manager over HTTP with
`app.MapWorkerManagerEndpoints()`, which maps `GET /workers`, `GET /workers/{id}`, and `POST /jobs`.

## How it works

The manager's `IJobScheduler` publishes a `JobRequest` onto the messaging queue. The worker runtime's consumer
service reads the queue, the dispatcher routes each request to the `IJobHandler` whose `JobName` matches, and the
handler runs the work. Messaging, workers, and the manager are independent modules that compose through the shared
DryIoc container.

## See also

- [SquidStd.Workers reference](../articles/workers.md)
- [SquidStd.Workers.Manager reference](../articles/workers-manager.md)
- [SquidStd.Workers.Abstractions reference](../articles/workers-abstractions.md)
- Previous: [Events, jobs and scheduling](events-jobs-scheduling.md)
