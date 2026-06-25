# Felix Network

**[Felix Network](https://github.com/tgiachi/SquidStd-Felix)** is a standalone companion library to
SquidStd: secure, cross-platform **binary mesh networking** for .NET — and for a constrained C
target such as the ESP32. It lives in its own repository and ships its own NuGet packages under the
`SquidStd.Felix.*` prefix.

## What it does

Felix sends **AES-256-GCM encrypted**, optionally **DEFLATE-compressed** binary messages between
nodes over **ENet** (reliable UDP), behind a small **portable binary frame** that a non-.NET target
can speak byte-for-byte. On top of that secure node it adds an optional self-forming **mesh** layer
(seed discovery + gossip of the peer list with auto-connect).

- **Secure node** — ENet transport, the portable frame (its header is authenticated as AES-GCM
  additional data), best-effort DEFLATE, and a raw `(type, bytes)` API.
- **Typed layer** — optional `Send<T>` / `On<T>` over MemoryPack with a `[FelixMessage(id)]`
  attribute mapping message types to wire ids.
- **Mesh** — fully-connected, self-healing peer discovery via seeds and portable gossip.
- **Pluggable transport** — the encrypted frame is transport-agnostic; `ITransport` decouples the
  node from the link (ENet ships; a serial/UART transport carries Felix over a reliable serial or
  Bluetooth RFCOMM stream without WiFi).
- **Portable** — the wire format is documented for non-.NET targets, with a host-testable C core
  (`felix-c`) and ESP32 (ESP-IDF) examples proving byte-for-byte C↔.NET parity.

## Packages

| Package | What it is |
|---------|------------|
| [`SquidStd.Felix`](https://www.nuget.org/packages/SquidStd.Felix/) | The secure node: ENet transport, portable frame codec, AES-256-GCM, best-effort DEFLATE, raw `SendRaw` / `OnRaw` API. |
| [`SquidStd.Felix.MemoryPack`](https://www.nuget.org/packages/SquidStd.Felix.MemoryPack/) | Optional typed layer: `Send<T>` / `On<T>` with `[FelixMessage(id)]`. |
| [`SquidStd.Felix.Mesh`](https://www.nuget.org/packages/SquidStd.Felix.Mesh/) | Self-forming full mesh: seed discovery + portable gossip of the peer list with auto-connect. |
| [`SquidStd.Felix.Transport.Serial`](https://www.nuget.org/packages/SquidStd.Felix.Transport.Serial/) | A stream-based serial/UART transport (`ITransport`) to run Felix over a reliable serial link. |

## Quick start

```bash
dotnet add package SquidStd.Felix
```

```csharp
using Felix;
using Felix.Data;
using Felix.Interfaces;

var key = new byte[32]; // 32-byte pre-shared key, shared by both ends

using IFelixNode listener = new FelixNode(new FelixOptions { ListenPort = 9000, PreSharedKey = key });
listener.OnRaw(7, (peer, bytes) => Console.WriteLine($"got {bytes.Length} bytes"));
listener.Start();

using IFelixNode client = new FelixNode(new FelixOptions { PreSharedKey = key });
client.Start();
FelixPeer peer = await client.ConnectAsync("127.0.0.1", 9000);
client.SendRaw(peer, type: 7, "hello"u8.ToArray());
```

### Typed messages (`SquidStd.Felix.MemoryPack`)

```csharp
[FelixMessage(7)]
[MemoryPackable]
public partial record Ping(long Ts);

listener.On<Ping>((peer, ping) => Console.WriteLine(ping.Ts));
client.Send(peer, new Ping(Environment.TickCount64));
```

### Mesh (`SquidStd.Felix.Mesh`)

```csharp
var node = new FelixNode(new FelixOptions { ListenPort = 9000, PreSharedKey = key });
using var mesh = new FelixMesh(node, new MeshOptions
{
    AdvertisedPort = 9000,
    Seeds = [ new MeshSeed("10.0.0.1", 9000) ],
});
mesh.MeshPeerJoined += p => Console.WriteLine($"join {p.Host}:{p.Port}");
mesh.Start();
mesh.Broadcast(type: 7, "hi mesh"u8.ToArray());
```

## ESP32 / C interop

The protocol is portable. The Felix repository ships a portable C core (`felix-c`: AES-256-GCM over
mbedTLS, raw-DEFLATE inflate, the Felix frame, an ENet leaf transport, and a `felix_serial` UART
module) plus host harnesses — all testable without an ESP32, proving byte-for-byte C↔.NET parity.
The same C builds on an ESP32 (ESP-IDF), over ENet/WiFi or over UART / Bluetooth Classic SPP.

## Learn more

See the [Felix Network repository](https://github.com/tgiachi/SquidStd-Felix) for the protocol
specification, the mesh protocol, the transport guide, and the ESP32 / C examples.
