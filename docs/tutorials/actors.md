# Actors

Build a single-consumer actor that mutates state without locks, then talk to it with
fire-and-forget and request/response messages.

## What you'll build

A `CounterActor` (`SquidStd.Actors`) that processes messages one at a time inside `ReceiveAsync`,
driven by `TellAsync` (fire-and-forget) and `AskAsync` (request/response).

## Prerequisites

- .NET 10 SDK
- `dotnet add package SquidStd.Actors`

## Steps

### 1. Define the message contract and the actor

A marker interface groups the messages; `Increment` is fire-and-forget, `GetTotal` derives from
`ActorRequest<int>` to carry a reply. The actor mutates `_total` without locks because messages
are processed one at a time.

[!code-csharp[](../../samples/SquidStd.Samples.Actors/Program.cs#step-1)]

### 2. Send fire-and-forget messages

`TellAsync` enqueues a message and returns without waiting for it to be handled.

[!code-csharp[](../../samples/SquidStd.Samples.Actors/Program.cs#step-2)]

### 3. Ask for a reply

`AskAsync<TRequest, TReply>` enqueues a request and awaits the typed reply the actor sends with
`request.Reply(...)`.

[!code-csharp[](../../samples/SquidStd.Samples.Actors/Program.cs#step-3)]

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.Actors
```

Prints `Total: 8`.

## Next steps

- [Messaging models](../articles/concepts/messaging-models.md)
- [SquidStd.Actors reference](../articles/actors.md)
