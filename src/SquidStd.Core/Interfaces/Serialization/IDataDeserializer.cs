namespace SquidStd.Core.Interfaces.Serialization;

/// <summary>
/// Deserializes bytes to typed values.
/// </summary>
public interface IDataDeserializer
{
    /// <summary>Deserializes bytes to a value.</summary>
    T Deserialize<T>(ReadOnlyMemory<byte> data);
}
