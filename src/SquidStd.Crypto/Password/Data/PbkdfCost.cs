namespace SquidStd.Crypto.Password.Data;

/// <summary>Argon2id cost parameters for password-based key derivation.</summary>
public sealed class PbkdfCost
{
    /// <summary>Argon2id memory cost in KiB.</summary>
    public int MemoryKib { get; }

    /// <summary>Argon2id iteration (time) cost.</summary>
    public int Iterations { get; }

    /// <summary>Argon2id parallelism (lanes).</summary>
    public int Parallelism { get; }

    public PbkdfCost(int memoryKib, int iterations, int parallelism = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(memoryKib);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(parallelism);

        MemoryKib = memoryKib;
        Iterations = iterations;
        Parallelism = parallelism;
    }

    /// <summary>Fast, for interactive logins (16 MiB, 2 passes).</summary>
    public static PbkdfCost Interactive { get; } = new(16 * 1024, 2);

    /// <summary>Balanced default (64 MiB, 3 passes).</summary>
    public static PbkdfCost Moderate { get; } = new(64 * 1024, 3);

    /// <summary>High cost for data at rest (256 MiB, 4 passes).</summary>
    public static PbkdfCost Sensitive { get; } = new(256 * 1024, 4);
}
