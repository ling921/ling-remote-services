using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Ling.RemoteServices.Generators;

internal static class GeneratorOptions
{
    public static IncrementalValueProvider<string> CreateRootNamespaceProvider(
        IncrementalGeneratorInitializationContext context)
    {
        return context.AnalyzerConfigOptionsProvider.Select(static (options, _) =>
            GetValue(options, "build_property.RootNamespace")
                ?? GetValue(options, "build_property.MSBuildProjectName")
                ?? "RemoteServices");
    }

    private static string? GetValue(AnalyzerConfigOptionsProvider options, string key)
    {
        return options.GlobalOptions.TryGetValue(key, out var value) ? value : null;
    }
}
