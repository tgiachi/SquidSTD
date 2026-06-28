# Command dispatcher

Register typed command handlers, dispatch commands against a context, and read back what matched.

## What you'll build

A `Container` with a `RegisterCommandDispatcher<Session>` and several handlers
(`SquidStd.Services.Core`), dispatching commands that carry a `Session` context. One command type
has two handlers — both run on dispatch.

## Prerequisites

- .NET 10 SDK
- `dotnet add package SquidStd.Services.Core`

## Steps

### 1. Register the dispatcher and handlers

Register the dispatcher for the context type, then each handler. `EchoCommand` has two handlers
(`EchoHandler` and `AuditHandler`); both will run. The subscription loop is what
`CommandDispatcherActivator<Session>` does at runtime — inlined here to keep the sample
self-contained.

[!code-csharp[](../../samples/SquidStd.Samples.Commands/Program.cs#step-1)]

### 2. Dispatch commands with a context

The context (here a `Session`) is passed explicitly at dispatch time — in a server this is the
session the message arrived on.

[!code-csharp[](../../samples/SquidStd.Samples.Commands/Program.cs#step-2)]

### 3. Read the dispatch result

`DispatchAsync` returns a result reporting whether any handler matched and how many ran. Nothing
is registered for `UnknownCommand`, so `Matched` is `false` and `HandlerCount` is `0`.

[!code-csharp[](../../samples/SquidStd.Samples.Commands/Program.cs#step-3)]

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.Commands
```

## Next steps

- [Messaging models](../articles/concepts/messaging-models.md)
