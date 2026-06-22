<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/SquidStd/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Messaging.RabbitMq</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Messaging.RabbitMq/"><img src="https://img.shields.io/nuget/v/SquidStd.Messaging.RabbitMq.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Messaging.RabbitMq.svg" alt="Downloads" />
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

RabbitMQ transport for SquidStd.Messaging. Implements `IQueueProvider` on top of the RabbitMQ client,
so the same `IMessageQueue` API publishes to and consumes from a real broker. Registered with a single
`AddRabbitMqMessaging(...)` call.

## Install

```bash
dotnet add package SquidStd.Messaging.RabbitMq
```

## Features

- One-line registration: `container.AddRabbitMqMessaging(connectionString)` or with `RabbitMqOptions`.
- Broker-backed `IQueueProvider` reusing the shared `IMessageQueue` facade and serializer.
- Connection via a `rabbitmq://` connection string or explicit `RabbitMqOptions` (host/port/vhost/credentials).
- Configurable consumer prefetch count (`?prefetch=` on the connection string, or `RabbitMqOptions.PrefetchCount`).

## Usage

```csharp
using DryIoc;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Messaging.RabbitMq.Extensions;

var container = new Container();
container.AddRabbitMqMessaging("rabbitmq://guest:guest@localhost:5672/");

var queue = container.Resolve<IMessageQueue>();
await queue.PublishAsync("orders", new { Id = 1 });
```

## Key types

| Type | Purpose |
|------|---------|
| `RabbitMqMessagingRegistrationExtensions` | `AddRabbitMqMessaging(...)` registration. |
| `RabbitMqQueueProvider` | RabbitMQ-backed `IQueueProvider`. |
| `RabbitMqOptions` | Connection + prefetch configuration. |

## License

MIT — part of [SquidStd](https://github.com/tgiachi/SquidStd).
