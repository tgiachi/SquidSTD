using System.Text;
using SquidStd.Core.Data.Bootstrap;
using SquidStd.Crypto.Vfs.Services;
using SquidStd.Services.Core.Services.Bootstrap;
using SquidStd.Vfs.Abstractions.Interfaces;
using SquidStd.Vfs.Extensions;
using SquidStd.Vfs.Services;

var bootstrap = SquidStdBootstrap.Create(
    new SquidStdOptions
    {
        ConfigName = "squidstd",
        RootDirectory = AppContext.BaseDirectory
    }
);

#region step-1

// Register a plain in-memory virtual filesystem.
bootstrap.ConfigureServices(container => container.RegisterVfs(_ => new InMemoryFileSystem()));

#endregion

await bootstrap.StartAsync();

#region step-2

// Write and read a file through the virtual filesystem.
var vfs = bootstrap.Resolve<IVirtualFileSystem>();

await vfs.WriteAllBytesAsync("notes/hello.txt", Encoding.UTF8.GetBytes("plain content"));
var bytes = await vfs.ReadAllBytesAsync("notes/hello.txt");

// The file was just written, so it is present (the API returns null only when absent).
Console.WriteLine($"VFS read: {Encoding.UTF8.GetString(bytes!)}");

#endregion

#region step-3

// Encrypted vault lifecycle: unlock -> write -> lock, then re-open and read.
// RegisterCryptoVault wires a DI vault over a single on-disk zip file, but that ZipFileSystem
// backend currently cannot be locked/persisted (its List reads ZipArchiveEntry.Length, which
// .NET marks unavailable in ZipArchiveMode.Update), so this sample drives CryptoFileSystem over
// an in-memory backend to demonstrate the full lifecycle without that limitation.
var backend = new InMemoryFileSystem();

using (var vault = new CryptoFileSystem(backend))
{
    vault.Unlock("vault passphrase");
    await vault.WriteAllBytesAsync("secret.txt", Encoding.UTF8.GetBytes("top secret"));
    vault.Lock(); // zeroes the key and flushes the encrypted index into the backend
}

// Re-open the same encrypted backend with the passphrase to prove the data round-trips at rest.
using (var reopened = new CryptoFileSystem(backend))
{
    reopened.Unlock("vault passphrase");

    // The secret was written above, so it is present.
    var secret = await reopened.ReadAllBytesAsync("secret.txt");
    Console.WriteLine($"Vault read after reopen: {Encoding.UTF8.GetString(secret!)}");
}

#endregion

await bootstrap.StopAsync();
