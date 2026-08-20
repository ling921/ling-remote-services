using Microsoft.CodeAnalysis;

namespace Ling.RemoteServices.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public sealed class RemoteServiceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var modeProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) => options.GlobalOptions.TryGetValue($"build_property.{Constants.RemoteServiceModePropertyName}", out var value)
                ? value.ToLowerInvariant()
                : null);

        var servicesProvider = context.CompilationProvider.Select((compilation, _) =>
        {
            var attributeTypeSymbol = compilation.GetTypeByMetadataName(Constants.RemoteServiceAttributeFullName);
            if (attributeTypeSymbol is null)
            {
                return default;
            }

            IAssemblySymbol[] assemblies = [compilation.Assembly, .. compilation.SourceModule.ReferencedAssemblySymbols];
            var interfaceSymbols = ScanForInterfaces(assemblies, attributeTypeSymbol).ToArray();
            return (compilation, interfaceSymbols);
        });

        var combinedProvider = servicesProvider.Combine(modeProvider);
        context.RegisterSourceOutput(combinedProvider, static (context, state) =>
        {
            var (servicesProvider, mode) = state;
            var (compilation, interfaceSymbols) = servicesProvider;

            // TODO: Generate code
        });
    }

    private static IEnumerable<INamedTypeSymbol> ScanForInterfaces(IAssemblySymbol[] assemblies, INamedTypeSymbol interfaceTypeSymbol)
    {
        // TODO: Scan for interfaces
        yield break;
    }
}
