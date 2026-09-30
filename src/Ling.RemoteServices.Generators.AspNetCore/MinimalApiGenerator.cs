using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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

        var duplicateEndpointNames = services
            .SelectMany(service => service.Methods.SelectMany(method => method.Operations.Select(operation =>
                (Name: ServerEmitter.GetOperationId(service, method, operation), Method: method.Symbol))))
            .GroupBy(item => item.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1);
        foreach (var duplicateEndpointName in duplicateEndpointNames)
        {
            foreach (var duplicate in duplicateEndpointName.Skip(1))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ContractDiagnostics.Invalid,
                    duplicate.Method.Locations.FirstOrDefault(),
                    $"Remote endpoint name '{duplicateEndpointName.Key}' is used more than once across mapped services. Endpoint names must be unique within an application."));
            }
        }

        var supportsDotNet10 = compilation.SyntaxTrees
            .Select(tree => tree.Options)
            .OfType<CSharpParseOptions>()
            .Any(options => options.PreprocessorSymbolNames.Contains("NET10_0_OR_GREATER", StringComparer.Ordinal));

        foreach (var service in services)
        {
            context.AddSource(
                GeneratorUtilities.GetHintName(service.Symbol, "Endpoints"),
                ServerEmitter.EmitService(service, rootNamespace, supportsDotNet10, context.ReportDiagnostic));
        }

        context.AddSource(
            "RemoteServiceEndpointExtensions.g.cs",
            ServerEmitter.EmitRegistration(services, rootNamespace));
    }
}
