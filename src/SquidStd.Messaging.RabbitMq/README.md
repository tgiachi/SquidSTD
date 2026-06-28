<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Messaging.RabbitMq</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Messaging.RabbitMq/"><img src="https://img.shields.io/nuget/v/SquidStd.Messaging.RabbitMq.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Messaging.RabbitMq.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/messaging-rabbitmq.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

RabbitMQ transport for SquidStd.Messaging. Implements `IQueueProvider` on top of the RabbitMQ client,
so the same `IMessageQueue` API publishes to and consumes from a real broker. Registered with a single
`AddRabbitMqMessaging(...)` call.

## Install

```bash
dotnet add package SquidStd.Messaging.RabbitMq
```

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

| Type                                      | Purpose                                   |
|-------------------------------------------|-------------------------------------------|
| `RabbitMqMessagingRegistrationExtensions` | `AddRabbitMqMessaging(...)` registration. |
| `RabbitMqQueueProvider`                   | RabbitMQ-backed `IQueueProvider`.         |
| `RabbitMqOptions`                         | Connection + prefetch configuration.      |

## Related

- Tutorial: [Messaging](https://tgiachi.github.io/squid-std/tutorials/messaging.html)

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
