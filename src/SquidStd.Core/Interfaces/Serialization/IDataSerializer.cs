namespace SquidStd.Core.Interfaces.Serialization;

/// <summary>
/// Serializes typed values to bytes.
/// </summary>
public interface IDataSerializer
{
    /// <summary>Serializes a value to bytes.</summary>
    ReadOnlyMemory<byte> Serialize<T>(T value);
}
