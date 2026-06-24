# TCP networking (server + client)

Stand up a TCP server, subscribe to its lifecycle and data events, then round-trip a message from a client.

## What you'll build

A `SquidTcpServer` (`SquidStd.Network`) bound to a loopback endpoint that logs client connections and received
payloads, plus a `SquidStdTcpClient` that connects and sends bytes. The server run is guarded behind `--run` so
`dotnet run` returns immediately by default.

## Prerequisites

- .NET 10 SDK
- `dotnet add package SquidStd.Network`
- A free local TCP port (the sample uses 9099 on loopback)

## Steps

### 1. Create the server and subscribe to events

Construct `SquidTcpServer` with an `IPEndPoint`, then subscribe to `OnClientConnect` and `OnDataReceived`. The data
event carries the source client and the payload as `ReadOnlyMemory<byte>`.

[!code-csharp[](../../samples/SquidStd.Samples.Networking/Program.cs#step-1)]

### 2. Start the server and connect a client

`StartAsync` begins accepting connections. `SquidStdTcpClient.ConnectAsync` opens an outbound connection and starts
its receive loop; `SendAsync` writes the payload, which surfaces on the server's `OnDataReceived`.

[!code-csharp[](../../samples/SquidStd.Samples.Networking/Program.cs#step-2)]

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.Networking -- --run
```

Without `--run` the program just prints a hint and exits. With `--run` it prints the connecting session and the
received message (`hello squid`).

## How it works

`SquidTcpServer` recreates its listening socket on every `StartAsync`, so Stop/Start cycles are supported. Each
accepted socket is wrapped in a `SquidStdTcpClient` whose events (`OnConnected`, `OnDataReceived`, `OnDisconnected`,
`OnException`) are re-raised by the server. An optional `INetFramer` lets you emit one `OnDataReceived` per logical
frame instead of per socket read, and `INetMiddleware` components can transform inbound and outbound payloads.

## See also

- [SquidStd.Network reference](../articles/network.md)
