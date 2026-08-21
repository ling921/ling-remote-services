using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Ling.RemoteServices.Generators;

/// <summary>
/// Generates ASP.NET Core Minimal API endpoint mappings.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class MinimalApiGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var sourceServices = ContractDiscovery.CreateSyntaxProvider(context).Collect();
        var rootNamespace = GeneratorOptions.CreateRootNamespaceProvider(context);
        var input = context.CompilationProvider.Combine(sourceServices).Combine(rootNamespace);

        context.RegisterSourceOutput(input, static (productionContext, value) =>
            Generate(productionContext, value.Left.Left, value.Left.Right, value.Right));
    }

    private static void Generate(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<INamedTypeSymbol> sourceServices,
        string rootNamespace)
    {
        var serviceSymbols = ContractDiscovery.FindAll(compilation, sourceServices);
        if (serviceSymbols.Count == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ContractDiagnostics.NoContracts,
                Location.None,
                "server"));
        }

        var services = serviceSymbols
            .Select(service => ContractParser.Parse(service))
            .Where(model => model is not null)
            .Cast<ServiceModel>()
            .ToList();

        foreach (var service in services)
        {
            context.AddSource(
                GeneratorUtilities.GetHintName(service.Symbol, "Endpoints"),
                ServerEmitter.EmitService(service, rootNamespace));
        }

        context.AddSource(
            "RemoteServiceEndpointExtensions.g.cs",
            ServerEmitter.EmitRegistration(services, rootNamespace));
    }
}
