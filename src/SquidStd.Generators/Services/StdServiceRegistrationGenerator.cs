using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SquidStd.Generators.Common;
using SquidStd.Generators.Diagnostics;

namespace SquidStd.Generators.Services;

[Generator(LanguageNames.CSharp)]
public sealed class StdServiceRegistrationGenerator : IIncrementalGenerator
{
    private const string AttributeName = "SquidStd.Abstractions.Attributes.RegisterStdServiceAttribute";
    private const string GeneratedFileName = "SquidStd.GeneratedStdServiceRegistration.g.cs";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeName,
            static (node, _) => node is ClassDeclarationSyntax,
            static (context, cancellationToken) => CreateCandidate(context, cancellationToken)
        );

        context.RegisterSourceOutput(candidates.Collect(), static (context, candidates) => Execute(context, candidates));
    }

    private static StdServiceRegistrationCandidate CreateCandidate(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var implementationType = (INamedTypeSymbol)context.TargetSymbol;
        var attribute = context.Attributes[0];
        var serviceType = GetServiceType(attribute);
        var priority = GetIntNamedArgument(attribute, "Priority");

        var isSupported = serviceType is not null
            && GeneratorSymbolHelpers.IsConcreteNonGenericClass(implementationType)
            && GeneratorSymbolHelpers.IsAccessibleFromGeneratedSource(implementationType)
            && GeneratorSymbolHelpers.IsAccessibleFromGeneratedSource(serviceType)
            && GeneratorSymbolHelpers.IsAssignableTo(implementationType, serviceType);

        return new(
            serviceType is null ? string.Empty : GeneratorSymbolHelpers.FullyQualified(serviceType),
            GeneratorSymbolHelpers.FullyQualified(implementationType),
            GeneratorSymbolHelpers.DisplayName(implementationType),
            GeneratorSymbolHelpers.PrimaryLocation(implementationType),
            priority,
            isSupported
        );
    }

    private static INamedTypeSymbol? GetServiceType(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length == 0)
        {
            return null;
        }

        return attribute.ConstructorArguments[0].Value as INamedTypeSymbol;
    }

    private static int GetIntNamedArgument(AttributeData attribute, string name)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == name && argument.Value.Value is int value)
            {
                return value;
            }
        }

        return 0;
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<StdServiceRegistrationCandidate> candidates)
    {
        var supported = new List<StdServiceRegistrationCandidate>();
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var candidate in candidates)
        {
            if (!candidate.IsSupported)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        SquidStdGeneratorDiagnostics.UnsupportedStdService,
                        candidate.Location,
                        candidate.DisplayName
                    )
                );

                continue;
            }

            var key = candidate.ServiceTypeName + "|" + candidate.ImplementationTypeName;
            if (seenKeys.Add(key))
            {
                supported.Add(candidate);
            }
        }

        supported.Sort(
            static (left, right) => string.Compare(
                left.ImplementationTypeName,
                right.ImplementationTypeName,
                StringComparison.Ordinal
            )
        );

        context.AddSource(GeneratedFileName, StdServiceSourceBuilder.Build(supported));
    }
}
