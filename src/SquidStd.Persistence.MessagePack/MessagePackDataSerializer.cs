using MessagePack;
using MessagePack.Resolvers;
using SquidStd.Core.Interfaces.Serialization;

namespace SquidStd.Persistence.MessagePack;

/// <summary>
/// MessagePack-backed binary <see cref="IDataSerializer" />/<see cref="IDataDeserializer" /> using the
/// contractless resolver, so plain POCOs need no attributes. Recommended binary default for persistence.
/// </summary>
public sealed class MessagePackDataSerializer : IDataSerializer, IDataDeserializer
{
    private readonly MessagePackSerializerOptions _options;

    public MessagePackDataSerializer()
    {
        _options = ContractlessStandardResolver.Options;
    }

    public ReadOnlyMemory<byte> Serialize<T>(T value)
        => MessagePackSerializer.Serialize(value, _options);

    public T Deserialize<T>(ReadOnlyMemory<byte> data)
        => MessagePackSerializer.Deserialize<T>(data, _options)!;
}
