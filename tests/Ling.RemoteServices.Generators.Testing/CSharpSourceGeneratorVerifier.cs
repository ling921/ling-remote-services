using Ling.RemoteServices.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Ling.RemoteServices.Generators.Testing;

/// <summary>
/// Creates an in-memory C# compilation and executes an incremental source generator.
/// </summary>
/// <typeparam name="TSourceGenerator">The incremental generator under test.</typeparam>
public static class CSharpSourceGeneratorVerifier<TSourceGenerator>
    where TSourceGenerator : IIncrementalGenerator, new()
{
    /// <summary>
    /// Runs the generator against the supplied C# source.
    /// </summary>
    /// <param name="source">The source used to create the test compilation.</param>
    /// <param name="additionalReferences">Additional references required by generated code.</param>
    /// <returns>The generator result and updated compilation.</returns>
    public static GeneratorTestRun Run(
        string source,
        params MetadataReference[] additionalReferences)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var references = GetFrameworkReferences()
            .Append(MetadataReference.CreateFromFile(typeof(RemoteServiceAttribute).Assembly.Location))
            .Concat(additionalReferences)
            .GroupBy(reference => reference.Display, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
        var compilation = CSharpCompilation.Create(
            "GeneratorFixtures",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { new TSourceGenerator().AsSourceGenerator() },
            parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out _);

        return new GeneratorTestRun(
            driver.GetRunResult().Results.Single(),
            outputCompilation);
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
