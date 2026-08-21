using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Ling.RemoteServices.Generators;

/// <summary>
/// Generates HTTP client proxies and their dependency injection registrations.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ClientProxyGenerator : IIncrementalGenerator
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
                "client"));
        }

        var services = serviceSymbols
            .Select(service => ContractParser.Parse(service))
            .Where(model => model is not null)
            .Cast<ServiceModel>()
            .ToList();

        foreach (var service in services)
        {
            context.AddSource(
                GeneratorUtilities.GetHintName(service.Symbol, "ClientProxy"),
                ClientEmitter.EmitService(service, rootNamespace));
        }

        context.AddSource(
            "RemoteServiceClientRegistrationExtensions.g.cs",
            ClientEmitter.EmitRegistration(services, rootNamespace));
    }
}
