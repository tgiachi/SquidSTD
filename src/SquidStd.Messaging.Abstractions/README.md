<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Messaging.Abstractions</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Messaging.Abstractions/"><img src="https://img.shields.io/nuget/v/SquidStd.Messaging.Abstractions.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Messaging.Abstractions.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/messaging-abstractions.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

Transport-agnostic messaging contracts for SquidStd. It defines the typed queue facade
(`IMessageQueue`), the low-level provider/serializer/metrics contracts, the message-listener interfaces,
and the shared `MessageQueue` facade plus default serializer and metrics. Pick a transport
implementation (in-memory or RabbitMQ) from a companion package.

## Install

```bash
dotnet add package SquidStd.Messaging.Abstractions
```

## Usage

```csharp
using SquidStd.Messaging.Abstractions.Interfaces;

public sealed class OrderCreated { public int Id { get; init; } }

public sealed class OrderListener : IQueueMessageListener<OrderCreated>
{
    public void Handle(OrderCreated message) { /* ... */ }
}

// Resolve IMessageQueue from a transport package (in-memory or RabbitMQ).
public async Task PublishAsync(IMessageQueue queue)
    => await queue.PublishAsync("orders", new OrderCreated { Id = 1 });
```

## Key types

| Type                       | Purpose                                                       |
|----------------------------|---------------------------------------------------------------|
| `IMessageQueue`            | Typed publish/subscribe facade.                               |
| `IQueueProvider`           | Transport contract (per backend).                             |
| `IMessageSerializer`       | Payload (de)serialization.                                    |
| `IQueueMessageListener<T>` | Subscriber callbacks.                                         |
| `IMessagingMetrics`        | Delivery metrics sink.                                        |
| `MessagingOptions`         | Delivery attempts, retry delay, and dead-letter-queue suffix. |

## Related

- Tutorial: [Messaging](https://tgiachi.github.io/squid-std/tutorials/messaging.html)

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
