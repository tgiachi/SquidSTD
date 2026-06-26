using SquidStd.Generators.Services;
using SquidStd.Tests.Generators.Support;

namespace SquidStd.Tests.Generators.Registration;

public class StdServiceRegistrationGeneratorTests
{
    [Fact]
    public void Run_GeneratesRegistrationExtension_WhenServiceIsAnnotated()
    {
        const string source = """
            using SquidStd.Abstractions.Attributes;

            namespace SampleApp;

            public interface ISampleService { }

            [RegisterStdService(typeof(ISampleService), Priority = 10)]
            public sealed class SampleService : ISampleService { }
            """;

        var result = GeneratorTestCompiler.Run(source, new StdServiceRegistrationGenerator());
        var generatedSource = SingleGeneratedSource(result, "SquidStd.GeneratedStdServiceRegistration.g.cs");

        Assert.Contains("RegisterGeneratedStdServices", generatedSource, StringComparison.Ordinal);
        Assert.Contains(
            "RegisterStdService<global::SampleApp.ISampleService, global::SampleApp.SampleService>(container, 10);",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Run_DoesNotRegisterService_WhenAttributeIsMissing()
    {
        const string source = """
            namespace SampleApp;

            public interface ISampleService { }
            public sealed class SampleService : ISampleService { }
            """;

        var result = GeneratorTestCompiler.Run(source, new StdServiceRegistrationGenerator());
        var generatedSource = SingleGeneratedSource(result, "SquidStd.GeneratedStdServiceRegistration.g.cs");

        Assert.Contains("RegisterGeneratedStdServices", generatedSource, StringComparison.Ordinal);
        Assert.DoesNotContain("RegisterStdService<", generatedSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_GeneratesNoOpExtension_WhenNoServicesExist()
    {
        const string source = """
            namespace SampleApp;

            public sealed class EmptyType { }
            """;

        var result = GeneratorTestCompiler.Run(source, new StdServiceRegistrationGenerator());
        var generatedSource = SingleGeneratedSource(result, "SquidStd.GeneratedStdServiceRegistration.g.cs");

        Assert.Contains("RegisterGeneratedStdServices", generatedSource, StringComparison.Ordinal);
        Assert.Contains("return container;", generatedSource, StringComparison.Ordinal);
        Assert.DoesNotContain("RegisterStdService<", generatedSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ReportsDiagnostic_WhenServiceContractIsMissing()
    {
        const string source = """
            using SquidStd.Abstractions.Attributes;

            namespace SampleApp;

            [RegisterStdService]
            public sealed class SampleService { }
            """;

        var result = GeneratorTestCompiler.Run(source, new StdServiceRegistrationGenerator());
        var diagnostics = result.RunResult.Diagnostics.Concat(result.Diagnostics);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "SQDGEN002");
    }

    private static string SingleGeneratedSource(
        (Microsoft.CodeAnalysis.Compilation Compilation,
         Microsoft.CodeAnalysis.GeneratorDriverRunResult RunResult,
         System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> Diagnostics) result,
        string fileName
    )
    {
        var generatedTree = Assert.Single(
            result.RunResult.GeneratedTrees,
            tree => tree.FilePath.EndsWith(fileName, StringComparison.Ordinal)
        );

        return generatedTree.GetText().ToString();
    }
}
