using Microsoft.CodeAnalysis;

namespace Ling.RemoteServices.Generators.Testing;

/// <summary>
/// Contains the generator result and the compilation produced by a generator test run.
/// </summary>
public sealed class GeneratorTestRun
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratorTestRun"/> class.
    /// </summary>
    /// <param name="generatorResult">The result reported for the tested generator.</param>
    /// <param name="outputCompilation">The compilation containing generated sources.</param>
    public GeneratorTestRun(
        GeneratorRunResult generatorResult,
        Compilation outputCompilation)
    {
        GeneratorResult = generatorResult;
        OutputCompilation = outputCompilation;
    }

    /// <summary>
    /// Gets the result reported for the tested generator.
    /// </summary>
    public GeneratorRunResult GeneratorResult { get; }

    /// <summary>
    /// Gets the compilation containing generated sources.
    /// </summary>
    public Compilation OutputCompilation { get; }
}
