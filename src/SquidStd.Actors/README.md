<h1 align="center">SquidStd.Actors</h1>

Ordered, single-threaded, lock-free per-entity message processing for SquidStd, built on TPL Dataflow
`ActionBlock<T>`. Each actor owns a mailbox processed by one consumer, so handler state is mutated
without locks. Complements the EventBus (stateless broadcast) and CommandDispatcher (stateless fan-out)
with ordered, stateful, per-instance processing.

## Install

```bash
dotnet add package SquidStd.Actors
```

## Usage

```csharp
using SquidStd.Actors;

public interface ISessionMessage { }

public sealed record LineReceived(string Raw) : ISessionMessage;
public sealed record SendText(string Text)    : ISessionMessage;
public sealed record GetNick : ActorRequest<string?>, ISessionMessage;   // request/response

public sealed class SessionActor : Actor<ISessionMessage>
{
    private readonly INetworkConnection _conn;
    private string? _nick;

    public SessionActor(INetworkConnection conn) { _conn = conn; }

    protected override async ValueTask ReceiveAsync(ISessionMessage message, CancellationToken ct)
    {
        switch (message)
        {
            case LineReceived(var raw): if (raw.StartsWith("NICK ")) _nick = raw[5..]; break;
            case SendText(var text):    await _conn.WriteAsync(text, ct); break;
            case GetNick q:             q.Reply(_nick); break;
        }
    }
}

await using var session = new SessionActor(conn);
await session.TellAsync(new LineReceived("NICK alice"));     // fire-and-forget
string? nick = await session.AskAsync<GetNick, string?>(new GetNick());   // request/response
```

Use the event bus as an ordered source:

```csharp
using SquidStd.Actors.Extensions;

using var sub = session.SubscribeToEventBus(eventBus, (UserJoinedEvent e) => new SendText($":{e.Nick} JOIN"));
```

## Key types

| Type                      | Purpose                                            |
|---------------------------|----------------------------------------------------|
| `Actor<TMessage>`         | Mailbox base class (`TellAsync` / `AskAsync`).     |
| `ActorRequest<TReply>`    | Base record for request/response messages.         |
| `IActorRequest<TReply>`   | Request contract (implement directly if not using the base). |
| `ActorOptions`            | Capacity / overflow / error configuration.         |

## Options

`ActorOptions` controls the mailbox:

| Property         | Values                              | Default  |
|------------------|-------------------------------------|----------|
| `Capacity`       | bounded mailbox size                | `1024`   |
| `OverflowPolicy` | `Wait` / `DropNewest` / `Unbounded` | `Wait`   |
| `ErrorPolicy`    | `Isolate` / `StopOnError`           | `Isolate`|

- **Wait**: `TellAsync` awaits until capacity frees (back-pressure).
- **DropNewest**: `TellAsync` returns `false` when full.
- **Isolate**: a throwing handler is logged and skipped; the actor stays alive. `AskAsync` exceptions
  always propagate to the caller regardless of policy.

`DisposeAsync` completes the mailbox, drains in-flight work, and faults any still-pending requests.

## Related

- Tutorial: [Actors](https://tgiachi.github.io/squid-std/tutorials/actors.html)
- Concept: [Messaging models](https://tgiachi.github.io/squid-std/articles/concepts/messaging-models.html)

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
