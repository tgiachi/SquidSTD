using SquidStd.Network.Interfaces.Codecs;
using SquidStd.Network.Interfaces.Framing;
using SquidStd.Network.Interfaces.Middleware;

namespace SquidStd.Network.Data;

/// <summary>
/// Per-connection transport configuration produced by a server factory on each accepted connection.
/// Any member left null falls back to the server's shared configuration.
/// </summary>
public sealed record ConnectionPipeline(
    ITransportCodec? Codec = null,
    IReadOnlyList<INetMiddleware>? Middlewares = null,
    INetFramer? Framer = null
);
