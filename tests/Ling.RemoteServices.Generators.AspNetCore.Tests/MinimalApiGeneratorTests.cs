using Ling.RemoteServices.AspNetCore;
using Ling.RemoteServices.Generators.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;

namespace Ling.RemoteServices.Generators.AspNetCore.Tests;

public class MinimalApiGeneratorTests
{
    [Fact]
    public void LRS004_reports_when_server_generation_has_no_contracts()
    {
        const string source = "namespace GeneratorFixtures; public sealed class Empty { }";

        var result = CSharpSourceGeneratorVerifier<MinimalApiGenerator>
            .Run(source)
            .GeneratorResult;
        var diagnostic = Assert.Single(
            result.Diagnostics,
            value => value.Id == "LRS004");

        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("server generation", diagnostic.GetMessage());
    }

    [Fact]
    public void Generator_emits_one_endpoint_group_file_per_service()
    {
        var run = CSharpSourceGeneratorVerifier<MinimalApiGenerator>.Run(
            GeneratorTestSources.Contracts,
            MetadataReference.CreateFromFile(
                typeof(RemoteServiceEndpointConventionRegistry).Assembly.Location),
            MetadataReference.CreateFromFile(
                typeof(OpenApiRouteHandlerBuilderExtensions).Assembly.Location));
        var result = run.GeneratorResult;
        var serviceSources = result.GeneratedSources
            .Where(source =>
                source.HintName.EndsWith(".Endpoints.g.cs", StringComparison.Ordinal))
            .Select(source => source.SourceText.ToString())
            .ToArray();

        AssertNoCompilerErrors(run.OutputCompilation);
        Assert.Equal(2, serviceSources.Length);
        Assert.All(result.GeneratedSources, source =>
            AssertGeneratedHeader(source.SourceText.ToString()));
        Assert.All(result.GeneratedSources, source =>
            AssertGeneratedTypeAttributes(source.SourceText.ToString()));
        Assert.Contains(serviceSources, source =>
            source.Contains("MapGroup(endpoints, \"/api/first\")", StringComparison.Ordinal)
            && source.Contains("\"\",", StringComparison.Ordinal));
        Assert.Contains(serviceSources, source =>
            source.Contains("MapGroup(endpoints, \"/api/second\")", StringComparison.Ordinal)
            && source.Contains("\"/items\",", StringComparison.Ordinal));
        Assert.All(serviceSources, source =>
            Assert.Contains("RemoteServiceEndpointConventionBuilder<", source));
        Assert.All(serviceSources, source =>
            Assert.Contains(
                "[global::Microsoft.AspNetCore.Mvc.FromServices]",
                source));
        Assert.All(serviceSources, source =>
            Assert.Contains(
                "RemoteServiceServerRuntime.GetJsonTypeInfo<",
                source));
        Assert.Contains(serviceSources, source => source.Contains(
            "operations.Add(\"GetAsync\", new global::Ling.RemoteServices.AspNetCore.RemoteServiceMethodConventionBuilder",
            StringComparison.Ordinal));
        Assert.Contains(serviceSources, source => source.Contains(
            "operations.Add(\"GetItemsAsync\", new global::Ling.RemoteServices.AspNetCore.RemoteServiceMethodConventionBuilder",
            StringComparison.Ordinal));
        Assert.Contains(serviceSources, source =>
            source.Contains("IFirstService_GetAsync_GET", StringComparison.Ordinal)
            && source.Contains("IFirstService_GetAsync_POST", StringComparison.Ordinal)
            && source.Contains(
                "WithSummary(\"Gets the first value.\")",
                StringComparison.Ordinal)
            && CountOccurrences(
                source,
                "EndpointRouteBuilderExtensions.MapMethods(") == 4);
        Assert.Contains(serviceSources, source =>
            source.Contains(
                "AuthorizationPolicyNames = new string?[] { \"ApiUser\" }",
                StringComparison.Ordinal)
            && source.Contains("AllowAnonymous = true", StringComparison.Ordinal)
            && source.Contains("CorsPolicyName = \"Frontend\"", StringComparison.Ordinal)
            && source.Contains(
                "OutputCachePolicyName = \"Weather\"",
                StringComparison.Ordinal)
            && source.Contains("RateLimitPolicyName = \"Reads\"", StringComparison.Ordinal)
            && source.Contains(
                "RequestTimeoutPolicyName = \"Fast\"",
                StringComparison.Ordinal)
            && source.Contains(
                "CustomPolicyNames = new string[] { \"ServicePolicy\", \"MethodPolicy\" }",
                StringComparison.Ordinal));

        var registration = GetSource(result, "RemoteServiceEndpointExtensions.g.cs");
        Assert.Equal(2, CountOccurrences(registration, "EndpointMapper.Map(endpoints)"));
        Assert.Contains(
            "RemoteServiceEndpointConventionRegistry MapRemoteServices",
            registration);
        Assert.Contains(
            "Action<global::Ling.RemoteServices.AspNetCore.RemoteServiceEndpointConventionRegistry>? configure",
            registration);
        Assert.Contains("configure?.Invoke(services)", registration);
    }

    private static string GetSource(GeneratorRunResult result, string hintName)
    {
        return Assert.Single(
                result.GeneratedSources,
                source => source.HintName == hintName)
            .SourceText
            .ToString();
    }

    private static int CountOccurrences(string value, string text)
    {
        var count = 0;
        var index = 0;

        while ((index = value.IndexOf(text, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += text.Length;
        }

        return count;
    }

    private static string NormalizeLineEndings(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static void AssertGeneratedHeader(string source)
    {
        var lines = NormalizeLineEndings(source).Split('\n');

        Assert.Equal("// <auto-generated/>", lines[0]);
        Assert.StartsWith("// Generated by Ling.RemoteServices v", lines[1]);
        Assert.EndsWith(".", lines[1]);
        Assert.Equal(string.Empty, lines[2]);
        Assert.Equal("#pragma warning disable CS1591", lines[3]);
        Assert.Equal(string.Empty, lines[4]);
        Assert.Equal("#nullable enable", lines[5]);
        Assert.Equal(string.Empty, lines[6]);
    }

    private static void AssertGeneratedTypeAttributes(string source)
    {
        Assert.Contains("System.CodeDom.Compiler.GeneratedCodeAttribute", source);
        Assert.Contains("System.Runtime.CompilerServices.CompilerGeneratedAttribute", source);
        Assert.Contains("System.Diagnostics.DebuggerNonUserCodeAttribute", source);
        Assert.Contains(
            "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute",
            source);
    }

    private static void AssertNoCompilerErrors(Compilation compilation)
    {
        Assert.Empty(compilation.GetDiagnostics().Where(diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error));
    }
}
