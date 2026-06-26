using SquidStd.Generators.Scripting.Lua;
using SquidStd.Tests.Generators.Support;

namespace SquidStd.Tests.Generators.Registration;

public class ScriptModuleRegistrationGeneratorTests
{
    [Fact]
    public void Run_GeneratesRegistrationExtension_WhenScriptModuleIsAnnotated()
    {
        const string source = """
            using SquidStd.Scripting.Lua.Attributes;
            using SquidStd.Scripting.Lua.Attributes.Scripts;

            namespace SampleApp;

            [RegisterScriptModule]
            [ScriptModule("sample")]
            public sealed class SampleScriptModule { }
            """;

        var result = GeneratorTestCompiler.Run(source, new ScriptModuleRegistrationGenerator());
        var generatedSource = SingleGeneratedSource(result, "SquidStd.GeneratedScriptModuleRegistration.g.cs");

        Assert.Contains("RegisterGeneratedScriptModules", generatedSource, StringComparison.Ordinal);
        Assert.Contains(
            "RegisterScriptModule<global::SampleApp.SampleScriptModule>(container);",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Run_DoesNotRegisterScriptModule_WhenRegisterAttributeIsMissing()
    {
        const string source = """
            using SquidStd.Scripting.Lua.Attributes.Scripts;

            namespace SampleApp;

            [ScriptModule("sample")]
            public sealed class SampleScriptModule { }
            """;

        var result = GeneratorTestCompiler.Run(source, new ScriptModuleRegistrationGenerator());
        var generatedSource = SingleGeneratedSource(result, "SquidStd.GeneratedScriptModuleRegistration.g.cs");

        Assert.Contains("RegisterGeneratedScriptModules", generatedSource, StringComparison.Ordinal);
        Assert.DoesNotContain("RegisterScriptModule<", generatedSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_GeneratesNoOpExtension_WhenNoScriptModulesExist()
    {
        const string source = """
            namespace SampleApp;

            public sealed class EmptyType { }
            """;

        var result = GeneratorTestCompiler.Run(source, new ScriptModuleRegistrationGenerator());
        var generatedSource = SingleGeneratedSource(result, "SquidStd.GeneratedScriptModuleRegistration.g.cs");

        Assert.Contains("RegisterGeneratedScriptModules", generatedSource, StringComparison.Ordinal);
        Assert.Contains("return container;", generatedSource, StringComparison.Ordinal);
        Assert.DoesNotContain("RegisterScriptModule<", generatedSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ReportsDiagnostic_WhenScriptModuleMetadataIsMissing()
    {
        const string source = """
            using SquidStd.Scripting.Lua.Attributes;

            namespace SampleApp;

            [RegisterScriptModule]
            public sealed class SampleScriptModule { }
            """;

        var result = GeneratorTestCompiler.Run(source, new ScriptModuleRegistrationGenerator());
        var diagnostics = result.RunResult.Diagnostics.Concat(result.Diagnostics);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "SQDGEN005");
    }

    [Fact]
    public void Run_ReportsDiagnostic_WhenScriptModuleIsAbstract()
    {
        const string source = """
            using SquidStd.Scripting.Lua.Attributes;
            using SquidStd.Scripting.Lua.Attributes.Scripts;

            namespace SampleApp;

            [RegisterScriptModule]
            [ScriptModule("sample")]
            public abstract class SampleScriptModule { }
            """;

        var result = GeneratorTestCompiler.Run(source, new ScriptModuleRegistrationGenerator());
        var diagnostics = result.RunResult.Diagnostics.Concat(result.Diagnostics);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "SQDGEN005");
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
