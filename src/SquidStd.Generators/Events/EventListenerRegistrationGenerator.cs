using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SquidStd.Generators.Diagnostics;

namespace SquidStd.Generators.Events;

[Generator(LanguageNames.CSharp)]
public sealed class EventListenerRegistrationGenerator : IIncrementalGenerator
{
    private const string EventListenerMetadataName = "IEventListener`1";
    private const string EventListenerNamespace = "SquidStd.Core.Interfaces.Events";
    private const string GeneratedFileName = "SquidStd.GeneratedEventListenerRegistration.g.cs";
    private const string RegisterEventListenerAttributeName =
        "SquidStd.Abstractions.Attributes.RegisterEventListenerAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidateGroups = context.SyntaxProvider.ForAttributeWithMetadataName(
            RegisterEventListenerAttributeName,
            static (node, _) => node is ClassDeclarationSyntax,
            static (context, cancellationToken) => CreateCandidates(context, cancellationToken)
        );

        context.RegisterSourceOutput(
            candidateGroups.Collect(),
            static (context, candidateGroups) => Execute(context, candidateGroups)
        );
    }

    private static ImmutableArray<EventListenerCandidate> CreateCandidates(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.TargetSymbol is not INamedTypeSymbol listenerType)
        {
            return ImmutableArray<EventListenerCandidate>.Empty;
        }

        if (listenerType.TypeKind != TypeKind.Class || listenerType.IsAbstract)
        {
            return ImmutableArray<EventListenerCandidate>.Empty;
        }

        var candidates = ImmutableArray.CreateBuilder<EventListenerCandidate>();

        for (var i = 0; i < listenerType.AllInterfaces.Length; i++)
        {
            var interfaceType = listenerType.AllInterfaces[i];
            if (!IsEventListenerInterface(interfaceType))
            {
                continue;
            }

            if (interfaceType.TypeArguments.Length != 1 || interfaceType.TypeArguments[0] is not INamedTypeSymbol eventType)
            {
                continue;
            }

            var isSupported = !listenerType.IsGenericType
                && IsAccessibleFromGeneratedSource(listenerType)
                && IsAccessibleFromGeneratedSource(eventType);

            candidates.Add(
                new EventListenerCandidate(
                    eventType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    listenerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    listenerType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    listenerType.Locations.FirstOrDefault(),
                    isSupported
                )
            );
        }

        return candidates.ToImmutable();
    }

    private static bool IsEventListenerInterface(INamedTypeSymbol interfaceType)
    {
        var originalDefinition = interfaceType.OriginalDefinition;

        return originalDefinition.MetadataName == EventListenerMetadataName
            && originalDefinition.ContainingNamespace.ToDisplayString() == EventListenerNamespace;
    }

    private static bool IsAccessibleFromGeneratedSource(INamedTypeSymbol type)
    {
        var current = type;

        while (current is not null)
        {
            if (current.DeclaredAccessibility is not Accessibility.Public and not Accessibility.Internal)
            {
                return false;
            }

            current = current.ContainingType;
        }

        return true;
    }

    private static void Execute(
        SourceProductionContext context,
        ImmutableArray<ImmutableArray<EventListenerCandidate>> candidateGroups
    )
    {
        var candidates = new List<EventListenerCandidate>();
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < candidateGroups.Length; i++)
        {
            var group = candidateGroups[i];

            for (var j = 0; j < group.Length; j++)
            {
                var candidate = group[j];

                if (!candidate.IsSupported)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            SquidStdGeneratorDiagnostics.UnsupportedEventListener,
                            candidate.Location,
                            candidate.DisplayName
                        )
                    );

                    continue;
                }

                var key = candidate.EventTypeName + "|" + candidate.ListenerTypeName;
                if (seenKeys.Add(key))
                {
                    candidates.Add(candidate);
                }
            }
        }

        candidates.Sort(
            static (left, right) => string.Compare(
                left.ListenerTypeName,
                right.ListenerTypeName,
                StringComparison.Ordinal
            )
        );

        context.AddSource(GeneratedFileName, EventListenerSourceBuilder.Build(candidates));
    }
}
