# Messaging: queues and pub/sub

Send work to a competing-consumers queue, and broadcast events to a fan-out topic.

## What you'll build

A host using `SquidStd.Messaging`: an `IMessageQueue` (one consumer handles each message) and an `IMessageTopic`
(every subscriber receives each message). The same APIs work over RabbitMQ via `SquidStd.Messaging.RabbitMq`.

## Prerequisites

- .NET 10 SDK
- `dotnet add package SquidStd.Messaging` (or `SquidStd.Messaging.RabbitMq`)

## Steps

### 1. Register in-memory messaging

[!code-csharp[](../../samples/SquidStd.Samples.Messaging/Program.cs#step-1)]

### 2. Queue: competing consumers

`Subscribe` a listener and `PublishAsync` to a named queue. Each message is handled by exactly one consumer.

[!code-csharp[](../../samples/SquidStd.Samples.Messaging/Program.cs#step-2)]

### 3. Topic: fan-out pub/sub

A topic delivers each published message to every current subscriber.

[!code-csharp[](../../samples/SquidStd.Samples.Messaging/Program.cs#step-3)]

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.Messaging
```

Prints `queue handled order-1` and `topic saw order-2`.

## How it works

Queues use competing-consumers with retry and dead-lettering (`MessagingOptions`); topics use transient fan-out
(at-most-once). Swap `AddInMemoryMessaging()` for `AddRabbitMqMessaging(...)` for a durable broker — the
`IMessageQueue`/`IMessageTopic` code is unchanged.

## See also

- [SquidStd.Messaging reference](../articles/messaging.md)
- [SquidStd.Messaging.RabbitMq reference](../articles/messaging-rabbitmq.md)
- Related: [A worker system end-to-end](worker-system.md)
