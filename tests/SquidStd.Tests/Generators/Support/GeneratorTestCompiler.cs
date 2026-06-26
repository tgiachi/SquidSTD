using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SquidStd.Abstractions.Extensions.Events;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Generators.Events;

namespace SquidStd.Tests.Generators.Support;

internal static class GeneratorTestCompiler
{
    public static (Compilation Compilation, GeneratorDriverRunResult RunResult, ImmutableArray<Diagnostic> Diagnostics) Run(string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
        var references = trustedPlatformAssemblies
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Cast<MetadataReference>()
            .ToList();

        references.Add(MetadataReference.CreateFromFile(typeof(IEvent).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(RegisterEventListenerExtension).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(DryIoc.IContainer).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "SquidStdGeneratorTests",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { new EventListenerRegistrationGenerator().AsSourceGenerator() },
            parseOptions: parseOptions
        );
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var updatedCompilation,
            out var diagnostics
        );

        return (updatedCompilation, driver.GetRunResult(), diagnostics);
    }
}
