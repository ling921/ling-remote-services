using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Ling.RemoteServices.Generators;

internal static class ContractDiscovery
{
    public static IncrementalValuesProvider<INamedTypeSymbol> CreateSyntaxProvider(
        IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider.ForAttributeWithMetadataName(
            ContractNames.ServiceAttribute,
            static (node, _) => node is InterfaceDeclarationSyntax,
            static (attributeContext, _) => (INamedTypeSymbol)attributeContext.TargetSymbol);
    }

    public static IReadOnlyList<INamedTypeSymbol> FindAll(
        Compilation compilation,
        ImmutableArray<INamedTypeSymbol> sourceServices)
    {
        var services = new List<INamedTypeSymbol>(sourceServices);

        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            foreach (var attribute in assembly.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != ContractNames.ManifestAttribute)
                {
                    continue;
                }

                if (attribute.ConstructorArguments.FirstOrDefault().Value is INamedTypeSymbol serviceType)
                {
                    services.Add(serviceType);
                }
            }
        }

        return services
            .GroupBy(service => service.ToDisplayString())
            .Select(group => group.First())
            .OrderBy(service => service.ToDisplayString(), StringComparer.Ordinal)
            .ToList();
    }
}
