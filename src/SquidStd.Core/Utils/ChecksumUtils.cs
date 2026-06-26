namespace SquidStd.Core.Utils;

/// <summary>
/// FNV-1a 32-bit non-cryptographic checksum used to validate persisted binary records.
/// </summary>
public static class ChecksumUtils
{
    private const uint FnvOffsetBasis = 2166136261;
    private const uint FnvPrime = 16777619;

    /// <summary>Computes the FNV-1a 32-bit checksum of the given bytes.</summary>
    public static uint Compute(ReadOnlySpan<byte> data)
    {
        var hash = FnvOffsetBasis;

        for (var i = 0; i < data.Length; i++)
        {
            hash ^= data[i];
            hash *= FnvPrime;
        }

        return hash;
    }
}
