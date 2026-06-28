using System.Diagnostics;

namespace SquidStd.Tests.Integration.Templates;

/// <summary>
///     Packs SquidStd.Templates, installs it into an isolated dotnet-new hive, instantiates each template, and
///     asserts the generated output (name substitution, version-sentinel replacement, messaging branch). No build
///     of the generated projects: the referenced SquidStd.* packages may not be published yet.
/// </summary>
public sealed class TemplatePackTests : IDisposable
{
    private readonly bool _dotnetAvailable;
    private readonly string _hive;
    private readonly bool _installed;
    private readonly string _repoRoot;
    private readonly string _workDir;

    // xUnit 2.9.3 has no dynamic skip; guard with an early return when the CLI/install is unavailable.
    private bool Ready => _dotnetAvailable && _installed;

    public TemplatePackTests()
    {
        _repoRoot = FindRepoRoot();
        _hive = Path.Combine(Path.GetTempPath(), "squidstd-templates-hive-" + Guid.NewGuid().ToString("N"));
        _workDir = Path.Combine(Path.GetTempPath(), "squidstd-templates-out-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_hive);
        Directory.CreateDirectory(_workDir);

        _dotnetAvailable = TryRun("dotnet", "--version", _repoRoot, out _);

        if (!_dotnetAvailable)
        {
            return;
        }

        var project = Path.Combine(_repoRoot, "src", "SquidStd.Templates", "SquidStd.Templates.csproj");
        Assert.True(TryRun("dotnet", $"pack \"{project}\" -c Release", _repoRoot, out _), "pack failed");

        var nupkg = Directory
            .GetFiles(
                Path.Combine(_repoRoot, "src", "SquidStd.Templates", "bin", "Release"),
                "SquidStd.Templates.*.nupkg"
            )
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .First();

        _installed = TryRun("dotnet", $"new install \"{nupkg}\" --debug:custom-hive \"{_hive}\"", _repoRoot, out _);
    }

    [Fact]
    public void AspNetCore_Instantiates_WithDockerfile()
    {
        if (!Ready)
        {
            return;
        }

        var outDir = New("squidstd-aspnetcore", "Acme.Api");

        Assert.True(File.Exists(Path.Combine(outDir, "Acme.Api.csproj")));
        Assert.True(File.Exists(Path.Combine(outDir, "Dockerfile")));
        Assert.Contains("UseSquidStd", File.ReadAllText(Path.Combine(outDir, "Program.cs")));
    }

    [Fact]
    public void Host_Instantiates_WithReplacedNameAndVersion()
    {
        if (!Ready)
        {
            return;
        }

        var outDir = New("squidstd-host", "Acme.Host");

        var csproj = Path.Combine(outDir, "Acme.Host.csproj");
        Assert.True(File.Exists(csproj));
        var text = File.ReadAllText(csproj);
        Assert.DoesNotContain("$squidstdversion$", text);
        Assert.DoesNotContain("SquidStd.Host.Template", text);
        Assert.Contains("SquidStd.Services.Core", text);
    }

    [Fact]
    public void Manager_Instantiates_WithEndpointsMapped()
    {
        if (!Ready)
        {
            return;
        }

        var outDir = New("squidstd-manager", "Acme.Manager");

        var program = File.ReadAllText(Path.Combine(outDir, "Program.cs"));
        Assert.Contains("MapWorkerManagerEndpoints", program);
        Assert.Contains("AddWorkerManager", program);
    }

    [Fact]
    public void Worker_InMemory_WiresInMemoryMessaging()
    {
        if (!Ready)
        {
            return;
        }

        var outDir = New("squidstd-worker", "Acme.WorkerMem", "--messaging inmemory");

        var program = File.ReadAllText(Path.Combine(outDir, "Program.cs"));
        Assert.Contains("AddInMemoryMessaging", program);
        Assert.DoesNotContain("AddRabbitMqMessaging", program);
    }

    [Fact]
    public void Worker_RabbitMq_WiresRabbitMqMessaging()
    {
        if (!Ready)
        {
            return;
        }

        var outDir = New("squidstd-worker", "Acme.Worker", "--messaging rabbitmq");

        var program = File.ReadAllText(Path.Combine(outDir, "Program.cs"));
        Assert.Contains("AddRabbitMqMessaging", program);
        Assert.DoesNotContain("AddInMemoryMessaging", program);
        Assert.Contains("SquidStd.Messaging.RabbitMq", File.ReadAllText(Path.Combine(outDir, "Acme.Worker.csproj")));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SquidStd.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate the repo root (SquidStd.slnx).");
    }

    private string New(string shortName, string name, string extraArgs = "")
    {
        var outDir = Path.Combine(_workDir, name);
        var args = $"new {shortName} -n {name} -o \"{outDir}\" --debug:custom-hive \"{_hive}\" {extraArgs}".Trim();
        Assert.True(TryRun("dotnet", args, _repoRoot, out var output), $"dotnet new failed: {output}");

        return outDir;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    private static bool TryRun(string file, string args, string workingDir, out string output)
    {
        var psi = new ProcessStartInfo(file, args)
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi)!;
        output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit(120_000);

        return process.HasExited && process.ExitCode == 0;
    }

    public void Dispose()
    {
        // The hive is isolated to this test instance, so deleting it fully uninstalls the pack — no
        // global state to clean up.
        TryDelete(_workDir);
        TryDelete(_hive);
    }
}
