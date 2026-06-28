using DryIoc;
using SquidStd.Crypto.Vfs.Extensions;
using SquidStd.Vfs.Abstractions.Interfaces;

namespace SquidStd.Tests.Crypto.Vfs;

public class RegisterCryptoVaultExtensionsTests
{
    [Fact]
    public void RegisterCryptoVault_RegistersLockableSingleton()
    {
        var path = Path.Combine(Path.GetTempPath(), "squidstd-vault-di-" + Guid.NewGuid().ToString("N") + ".dat");

        try
        {
            using var container = new Container();
            container.RegisterCryptoVault(path);

            var vault = container.Resolve<ILockableFileSystem>();
            Assert.False(vault.IsUnlocked);
            Assert.Same(vault, container.Resolve<ILockableFileSystem>());
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
