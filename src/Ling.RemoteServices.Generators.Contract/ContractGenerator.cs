using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using static Ling.RemoteServices.Generators.GeneratorUtilities;

namespace Ling.RemoteServices.Generators;

/// <summary>
/// Validates remote service contracts and emits their assembly manifest.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ContractGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var sourceServices = ContractDiscovery.CreateSyntaxProvider(context).Collect();
        context.RegisterSourceOutput(sourceServices, static (productionContext, services) =>
            Generate(productionContext, services));
    }

    private static void Generate(
        SourceProductionContext context,
        ImmutableArray<INamedTypeSymbol> sourceServices)
    {
        if (sourceServices.IsDefaultOrEmpty)
        {
            return;
        }

        var source = CreateGeneratedCodeBuilder();
        foreach (var service in sourceServices
            .GroupBy(symbol => symbol.ToDisplayString())
            .Select(group => group.First()))
        {
            var model = ContractParser.Parse(service);
            if (model is null)
            {
                continue;
            }

            source
                .Append("[assembly: global::Ling.RemoteServices.Attributes.RemoteServiceContractAttribute(typeof(")
                .Append(TypeName(service))
                .AppendLine("))]");

            foreach (var method in model.Methods.Where(method => method.Summary is not null))
            {
                source
                    .Append("[assembly: global::Ling.RemoteServices.Attributes.RemoteServiceMethodDocumentationAttribute(typeof(")
                    .Append(TypeName(service))
                    .Append("), \"")
                    .Append(Escape(method.Symbol.Name))
                    .Append("\", \"")
                    .Append(Escape(method.Summary!))
                    .AppendLine("\")]");
            }
        }

        context.AddSource(
            "RemoteServiceContracts.g.cs",
            source.ToSourceText());
    }
}
