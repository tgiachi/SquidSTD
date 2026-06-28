<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Network</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Network/"><img src="https://img.shields.io/nuget/v/SquidStd.Network.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Network.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/network.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

Networking primitives for SquidStd: TCP and UDP servers and clients with per-connection sessions, a
pluggable framing + middleware pipeline, span-based binary readers/writers, and a circular buffer —
designed for low-allocation, high-throughput byte processing.

## Install

```bash
dotnet add package SquidStd.Network
```

## Usage

```csharp
using System.Net;
using SquidStd.Network.Server;

await using var server = new SquidTcpServer(new IPEndPoint(IPAddress.Any, 9000));
await server.StartAsync(CancellationToken.None);
// ... server.IsRunning, server.Port ...
await server.StopAsync(CancellationToken.None);
```

## Key types

| Type                        | Purpose                                             |
|-----------------------------|-----------------------------------------------------|
| `INetworkServer`            | TCP/UDP server contract (`StartAsync`/`StopAsync`). |
| `INetworkConnection`        | A single client connection.                         |
| `ISessionManager<TState>`   | Tracks sessions and their typed state.              |
| `INetFramer`                | Splits the byte stream into messages.               |
| `INetMiddleware`            | Pipeline stage over inbound/outbound data.          |
| `SpanReader` / `SpanWriter` | Allocation-free binary read/write.                  |

## Related

- Tutorial: [Networking](https://tgiachi.github.io/squid-std/tutorials/networking.html)

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
