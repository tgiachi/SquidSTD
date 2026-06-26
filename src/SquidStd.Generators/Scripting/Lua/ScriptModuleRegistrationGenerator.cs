using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SquidStd.Generators.Common;
using SquidStd.Generators.Diagnostics;

namespace SquidStd.Generators.Scripting.Lua;

[Generator(LanguageNames.CSharp)]
public sealed class ScriptModuleRegistrationGenerator : IIncrementalGenerator
{
    private const string AttributeName = "SquidStd.Scripting.Lua.Attributes.RegisterScriptModuleAttribute";
    private const string GeneratedFileName = "SquidStd.GeneratedScriptModuleRegistration.g.cs";
    private const string ScriptModuleAttributeMetadataName = "ScriptModuleAttribute";
    private const string ScriptModuleAttributeNamespace = "SquidStd.Scripting.Lua.Attributes.Scripts";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeName,
            static (node, _) => node is ClassDeclarationSyntax,
            static (context, cancellationToken) => CreateCandidate(context, cancellationToken)
        );

        context.RegisterSourceOutput(candidates.Collect(), static (context, candidates) => Execute(context, candidates));
    }

    private static ScriptModuleRegistrationCandidate CreateCandidate(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var scriptModuleType = (INamedTypeSymbol)context.TargetSymbol;
        var isSupported = GeneratorSymbolHelpers.IsConcreteNonGenericClass(scriptModuleType)
            && GeneratorSymbolHelpers.IsAccessibleFromGeneratedSource(scriptModuleType)
            && HasScriptModuleAttribute(scriptModuleType);

        return new(
            GeneratorSymbolHelpers.FullyQualified(scriptModuleType),
            GeneratorSymbolHelpers.DisplayName(scriptModuleType),
            GeneratorSymbolHelpers.PrimaryLocation(scriptModuleType),
            isSupported
        );
    }

    private static bool HasScriptModuleAttribute(INamedTypeSymbol type)
        => type.GetAttributes().Any(
            attribute =>
            {
                var attributeClass = attribute.AttributeClass;

                return attributeClass is not null
                    && attributeClass.MetadataName == ScriptModuleAttributeMetadataName
                    && attributeClass.ContainingNamespace.ToDisplayString() == ScriptModuleAttributeNamespace;
            }
        );

    private static void Execute(
        SourceProductionContext context,
        ImmutableArray<ScriptModuleRegistrationCandidate> candidates
    )
    {
        var supported = new List<ScriptModuleRegistrationCandidate>();
        var seenScriptModuleTypes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var candidate in candidates)
        {
            if (!candidate.IsSupported)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        SquidStdGeneratorDiagnostics.UnsupportedScriptModule,
                        candidate.Location,
                        candidate.DisplayName
                    )
                );

                continue;
            }

            if (seenScriptModuleTypes.Add(candidate.ScriptModuleTypeName))
            {
                supported.Add(candidate);
            }
        }

        supported.Sort(
            static (left, right) => string.Compare(
                left.ScriptModuleTypeName,
                right.ScriptModuleTypeName,
                StringComparison.Ordinal
            )
        );

        context.AddSource(GeneratedFileName, ScriptModuleSourceBuilder.Build(supported));
    }
}
