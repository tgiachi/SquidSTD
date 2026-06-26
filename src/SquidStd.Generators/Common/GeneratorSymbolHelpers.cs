using Microsoft.CodeAnalysis;

namespace SquidStd.Generators.Common;

internal static class GeneratorSymbolHelpers
{
    public static string FullyQualified(ITypeSymbol symbol)
        => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public static string DisplayName(ISymbol symbol)
        => symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

    public static Location? PrimaryLocation(ISymbol symbol)
        => symbol.Locations.FirstOrDefault();

    public static bool IsConcreteNonGenericClass(INamedTypeSymbol type)
        => type.TypeKind == TypeKind.Class && !type.IsAbstract && !type.IsGenericType;

    public static bool IsAccessibleFromGeneratedSource(INamedTypeSymbol type)
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

    public static bool ImplementsInterface(INamedTypeSymbol type, string metadataName, string namespaceName)
        => type.AllInterfaces.Any(
            interfaceType =>
            {
                var originalDefinition = interfaceType.OriginalDefinition;

                return originalDefinition.MetadataName == metadataName
                    && originalDefinition.ContainingNamespace.ToDisplayString() == namespaceName;
            }
        );

    public static bool IsAssignableTo(INamedTypeSymbol implementationType, INamedTypeSymbol serviceType)
    {
        if (SymbolEqualityComparer.Default.Equals(implementationType, serviceType))
        {
            return true;
        }

        for (var current = implementationType.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, serviceType))
            {
                return true;
            }
        }

        return implementationType.AllInterfaces.Any(
            interfaceType => SymbolEqualityComparer.Default.Equals(interfaceType, serviceType)
        );
    }

    public static bool HasPublicParameterlessConstructor(INamedTypeSymbol type)
        => type.InstanceConstructors.Any(
            constructor => constructor.Parameters.Length == 0 && constructor.DeclaredAccessibility == Accessibility.Public
        );
}
