using System.Collections.Immutable;
using Ling.RemoteServices.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Ling.RemoteServices.Generators.Testing;

/// <summary>
/// Creates an in-memory C# compilation and executes a diagnostic analyzer.
/// </summary>
/// <typeparam name="TAnalyzer">The diagnostic analyzer under test.</typeparam>
public static class CSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    /// <summary>
    /// Runs the analyzer against the supplied C# source.
    /// </summary>
    /// <param name="source">The source used to create the test compilation.</param>
    /// <param name="additionalReferences">Additional references required by the source.</param>
    /// <returns>The diagnostics reported by the analyzer.</returns>
    public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
        string source,
        params MetadataReference[] additionalReferences)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
        var references = GetFrameworkReferences()
            .Append(MetadataReference.CreateFromFile(typeof(RemoteServiceAttribute).Assembly.Location))
            .Concat(additionalReferences)
            .GroupBy(reference => reference.Display, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
        var compilation = CSharpCompilation.Create(
            "AnalyzerFixtures",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new TAnalyzer());

        return await compilation
            .WithAnalyzers(analyzers)
            .GetAnalyzerDiagnosticsAsync()
            .ConfigureAwait(false);
    }

    private static IEnumerable<MetadataReference> GetFrameworkReferences()
    {
        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException(
                "The trusted platform assembly list is unavailable.");

        return trustedAssemblies
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
    }
}
