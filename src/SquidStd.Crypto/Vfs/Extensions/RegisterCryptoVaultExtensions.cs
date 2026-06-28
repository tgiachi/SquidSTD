using DryIoc;
using SquidStd.Crypto.Vfs.Data;
using SquidStd.Crypto.Vfs.Services;
using SquidStd.Vfs.Abstractions.Interfaces;
using SquidStd.Vfs.Services;

namespace SquidStd.Crypto.Vfs.Extensions;

/// <summary>DryIoc registration helpers for an encrypted single-file vault.</summary>
public static class RegisterCryptoVaultExtensions
{
    /// <param name="container">Container that receives the vault registration.</param>
    extension(IContainer container)
    {
        /// <summary>
        ///     Registers an <see cref="ILockableFileSystem" /> singleton: a crypto vault over a single-file zip at
        ///     <paramref name="path" />. The consumer calls <see cref="ILockableFileSystem.Unlock" /> at runtime.
        /// </summary>
        public IContainer RegisterCryptoVault(string path, CryptoVaultOptions? options = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            container.RegisterDelegate<ILockableFileSystem>(
                _ => new CryptoFileSystem(new ZipFileSystem(path), options),
                Reuse.Singleton
            );

            return container;
        }
    }
}
