using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SquidStd.Messaging.Abstractions.Interfaces;

namespace SquidStd.Messaging.Abstractions;

/// <summary>
/// Default JSON message serializer based on System.Text.Json.
/// </summary>
public sealed class JsonMessageSerializer : IMessageSerializer
{
    private readonly JsonSerializerOptions _options;

    public JsonMessageSerializer()
    {
        _options = new(JsonSerializerDefaults.Web);
    }

    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization may require runtime code generation.")]
    public ReadOnlyMemory<byte> Serialize<TMessage>(TMessage message)
        => JsonSerializer.SerializeToUtf8Bytes(message, _options);

    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization may require runtime code generation.")]
    public TMessage Deserialize<TMessage>(ReadOnlyMemory<byte> payload)
        => JsonSerializer.Deserialize<TMessage>(payload.Span, _options) ??
           throw new InvalidOperationException($"Deserialization returned null for type {typeof(TMessage).Name}.");
}
