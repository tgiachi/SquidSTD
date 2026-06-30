using System.Text;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;

namespace SquidStd.Crypto.Password.Internal;

/// <summary>Derives a 32-byte key from a password with Argon2id (v1.3).</summary>
internal static class PasswordKeyDerivation
{
    public static byte[] DeriveKey(string password, byte[] salt, int iterations, int memoryKib, int parallelism)
    {
        var parameters = new Argon2Parameters.Builder(Argon2Parameters.Argon2id)
                         .WithVersion(Argon2Parameters.Version13)
                         .WithSalt(salt)
                         .WithIterations(iterations)
                         .WithMemoryAsKB(memoryKib)
                         .WithParallelism(parallelism)
                         .Build();

        var generator = new Argon2BytesGenerator();
        generator.Init(parameters);

        var key = new byte[32];
        generator.GenerateBytes(Encoding.UTF8.GetBytes(password), key);

        return key;
    }
}
