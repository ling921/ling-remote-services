using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Ling.RemoteServices.Generators;

/// <summary>
/// Analyzes remote service interfaces and reports invalid contract declarations.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RemoteServiceContractAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        ContractDiagnostics.AsyncRequired,
        ContractDiagnostics.MissingHttpMethod,
        ContractDiagnostics.Invalid,
        ContractDiagnostics.ClientDefaultRequired,
        ContractDiagnostics.MultipleClientDefaults,
        ContractDiagnostics.DuplicateHttpOperation
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var service = (INamedTypeSymbol)context.Symbol;
        if (!service.GetAttributes().Any(attribute =>
                attribute.AttributeClass?.ToDisplayString() == ContractNames.ServiceAttribute))
        {
            return;
        }

        _ = ContractParser.Parse(service, context.ReportDiagnostic);
    }
}
