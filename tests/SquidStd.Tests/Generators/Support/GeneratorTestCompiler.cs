using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SquidStd.Abstractions.Attributes;
using SquidStd.Abstractions.Extensions.Config;
using SquidStd.Abstractions.Extensions.Events;
using SquidStd.Abstractions.Extensions.Services;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Generators.Events;
using SquidStd.Scripting.Lua.Attributes;
using SquidStd.Scripting.Lua.Extensions.Scripts;
using SquidStd.Workers.Abstractions.Data;
using SquidStd.Workers.Attributes;
using SquidStd.Workers.Extensions;
using SquidStd.Workers.Interfaces;

namespace SquidStd.Tests.Generators.Support;

internal static class GeneratorTestCompiler
{
    public static (Compilation Compilation, GeneratorDriverRunResult RunResult, ImmutableArray<Diagnostic> Diagnostics) Run(
        string source,
        params IIncrementalGenerator[] generators
    )
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        var references = CreateReferences();

        var compilation = CSharpCompilation.Create(
            "SquidStdGeneratorTests",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        ISourceGenerator[] selectedGenerators;
        if (generators.Length == 0)
        {
            selectedGenerators = new[] { new EventListenerRegistrationGenerator().AsSourceGenerator() };
        }
        else
        {
            selectedGenerators = generators.Select(generator => generator.AsSourceGenerator()).ToArray();
        }

        GeneratorDriver driver = CSharpGeneratorDriver.Create(selectedGenerators, parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var updatedCompilation,
            out var diagnostics
        );

        return (updatedCompilation, driver.GetRunResult(), diagnostics);
    }

    private static IReadOnlyList<MetadataReference> CreateReferences()
    {
        var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
        var references = trustedPlatformAssemblies
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Cast<MetadataReference>()
            .ToList();

        AddReference(references, typeof(IEvent).Assembly.Location);
        AddReference(references, typeof(RegisterEventListenerAttribute).Assembly.Location);
        AddReference(references, typeof(RegisterConfigSectionExtension).Assembly.Location);
        AddReference(references, typeof(RegisterEventListenerExtension).Assembly.Location);
        AddReference(references, typeof(RegisterStdServiceExtension).Assembly.Location);
        AddReference(references, typeof(RegisterScriptModuleAttribute).Assembly.Location);
        AddReference(references, typeof(AddScriptModuleExtension).Assembly.Location);
        AddReference(references, typeof(JobRequest).Assembly.Location);
        AddReference(references, typeof(RegisterJobHandlerAttribute).Assembly.Location);
        AddReference(references, typeof(WorkersRegistrationExtensions).Assembly.Location);
        AddReference(references, typeof(IJobHandler).Assembly.Location);
        AddReference(references, typeof(DryIoc.IContainer).Assembly.Location);

        return references;
    }

    private static void AddReference(List<MetadataReference> references, string location)
    {
        if (references.Any(reference => string.Equals(reference.Display, location, StringComparison.Ordinal)))
        {
            return;
        }

        references.Add(MetadataReference.CreateFromFile(location));
    }
}
