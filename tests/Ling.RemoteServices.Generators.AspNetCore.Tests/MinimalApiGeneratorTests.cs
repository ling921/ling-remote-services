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
    public void Generator_reports_duplicate_endpoint_names_across_services()
    {
        const string source = """
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            [RemoteService("/first")]
            public interface IFirstService
            {
                [Get]
                [RemoteEndpointName("shared.endpoint")]
                Task<string> GetAsync();
            }

            [RemoteService("/second")]
            public interface ISecondService
            {
                [Get]
                [RemoteEndpointName("shared.endpoint")]
                Task<string> GetAsync();
            }
            """;

        var result = CSharpSourceGeneratorVerifier<MinimalApiGenerator>
            .Run(source)
            .GeneratorResult;
        var diagnostic = Assert.Single(result.Diagnostics, item => item.Id == "LRS003");

        Assert.Contains("shared.endpoint", diagnostic.GetMessage());
        Assert.Contains("must be unique within an application", diagnostic.GetMessage());
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
            && source.Contains(
                "AuthorizationRoleGroups = new string[] { \"Admin, Operator\" }",
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

    [Fact]
    public void Generator_preserves_role_groups_and_method_authorization_overrides_anonymous_service()
    {
        const string source = """
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            namespace GeneratorFixtures;

            [RemoteService("/api/roles")]
            [RemoteAllowAnonymous]
            [RemoteAuthorize(Roles = "Admin,Operator")]
            public interface IRoleService
            {
                [Get]
                [RemoteAuthorize(Roles = "Auditor")]
                Task<string> GetAsync();
            }
            """;

        var run = CSharpSourceGeneratorVerifier<MinimalApiGenerator>.Run(
            source,
            MetadataReference.CreateFromFile(
                typeof(RemoteServiceEndpointConventionRegistry).Assembly.Location),
            MetadataReference.CreateFromFile(
                typeof(OpenApiRouteHandlerBuilderExtensions).Assembly.Location));
        var generated = Assert.Single(
                run.GeneratorResult.GeneratedSources,
                generatedSource => generatedSource.HintName.EndsWith(
                    ".Endpoints.g.cs",
                    StringComparison.Ordinal))
            .SourceText
            .ToString();

        AssertNoCompilerErrors(run.OutputCompilation);
        Assert.Contains(
            "AuthorizationRoleGroups = new string[] { \"Admin,Operator\", \"Auditor\" }",
            generated);
        Assert.DoesNotContain("AuthorizationPolicyNames =", generated);
        Assert.DoesNotContain("AllowAnonymous = true", generated);
    }

    [Fact]
    public void Generator_inherits_base_interface_endpoint_configuration_and_parameter_binding()
    {
        const string source = """
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            namespace GeneratorFixtures;

            public interface IBaseCatalogService
            {
                [Get]
                [RemoteSummary("Base summary")]
                [RemoteProduces<string>(201, "application/vnd.catalog+json")]
                [RemoteDescription("Base description")]
                [RemoteExcludeFromDescription]
                [RemoteHost("catalog.example.com")]
                [RemoteOrder(3)]
                [RemoteDisplayName("Catalog lookup")]
                [RemoteEndpointName("catalog.find")]
                [RemoteShortCircuit(418)]
                Task<string> FindAsync([Query("base-query")] string originalName);
            }

            [RemoteService("/api/catalog")]
            [RemoteTags("catalog")]
            public interface ICatalogService : IBaseCatalogService
            {
                new Task<string> FindAsync(string renamedName);

                [Post("upload")]
                [RemoteFormOptions(ValueCountLimit = 10)]
                [RemoteFormMappingOptions(MaxCollectionSize = 20, MaxRecursionDepth = 12, MaxKeySize = 256)]
                Task<string> UploadAsync([Form] string title);
            }
            """;

        var run = CSharpSourceGeneratorVerifier<MinimalApiGenerator>.Run(
            source,
            MetadataReference.CreateFromFile(typeof(RemoteServiceEndpointConventionRegistry).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(OpenApiRouteHandlerBuilderExtensions).Assembly.Location));
        var generated = Assert.Single(
                run.GeneratorResult.GeneratedSources,
                item => item.HintName.EndsWith(".Endpoints.g.cs", StringComparison.Ordinal))
            .SourceText
            .ToString();

        AssertNoCompilerErrors(run.OutputCompilation);
        Assert.Contains("FromQuery(Name=\"base-query\")] string renamedName", generated);
        Assert.Contains("WithSummary<global::Ling.RemoteServices.AspNetCore.RemoteServiceOperationConventionBuilder>(operation0, \"Base summary\")", generated);
        Assert.Contains("ProducesResponseTypeMetadata(201, typeof(string)", generated);
        Assert.Contains("WithTags<global::Ling.RemoteServices.AspNetCore.RemoteServiceOperationConventionBuilder>", generated);
        Assert.Contains("FindAsync(renamedName)", generated);
        Assert.Contains("WithDescription<global::Ling.RemoteServices.AspNetCore.RemoteServiceOperationConventionBuilder>(operation0, \"Base description\")", generated);
        Assert.Contains("ExcludeFromDescription<global::Ling.RemoteServices.AspNetCore.RemoteServiceOperationConventionBuilder>(operation0)", generated);
        Assert.Contains("RequireHost<global::Ling.RemoteServices.AspNetCore.RemoteServiceOperationConventionBuilder>(operation0, new string[] { \"catalog.example.com\" })", generated);
        Assert.Contains("WithOrder<global::Ling.RemoteServices.AspNetCore.RemoteServiceOperationConventionBuilder>(operation0, 3)", generated);
        Assert.Contains("WithDisplayName<global::Ling.RemoteServices.AspNetCore.RemoteServiceOperationConventionBuilder>(operation0, \"Catalog lookup\")", generated);
        Assert.Contains("WithName(\"catalog.find\")", generated);
        Assert.Contains("RouteShortCircuitEndpointConventionBuilderExtensions.ShortCircuit(operation0, 418)", generated);
        Assert.Contains("WithFormOptions<global::Ling.RemoteServices.AspNetCore.RemoteServiceOperationConventionBuilder>(operation1, valueCountLimit: 10)", generated);
        Assert.Contains("WithFormMappingOptions<global::Ling.RemoteServices.AspNetCore.RemoteServiceOperationConventionBuilder>(operation1, maxCollectionSize: 20, maxRecursionDepth: 12, maxKeySize: 256)", generated);
    }

    [Fact]
    public void Generator_applies_selector_based_metadata_only_to_selected_http_operation()
    {
        const string source = """
            using Ling.RemoteServices;
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            namespace GeneratorFixtures;

            [RemoteService("/api/selector")]
            public interface ISelectorService
            {
                [Get(IsClientDefault = true), Post]
                [RemoteSummary("GET only", HttpMethod = RemoteHttpMethod.Get)]
                [RemoteAuthorize("GetPolicy", HttpMethod = RemoteHttpMethod.Get)]
                [RemoteRequestTimeout(250, HttpMethod = RemoteHttpMethod.Post)]
                [RemoteProducesProblem(422, HttpMethod = RemoteHttpMethod.Post)]
                Task<string> ExecuteAsync();
            }
            """;

        var run = CSharpSourceGeneratorVerifier<MinimalApiGenerator>.Run(
            source,
            MetadataReference.CreateFromFile(typeof(RemoteServiceEndpointConventionRegistry).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(OpenApiRouteHandlerBuilderExtensions).Assembly.Location));
        var generated = Assert.Single(
                run.GeneratorResult.GeneratedSources,
                item => item.HintName.EndsWith(".Endpoints.g.cs", StringComparison.Ordinal))
            .SourceText
            .ToString();

        AssertNoCompilerErrors(run.OutputCompilation);
        var firstOperationStart = generated.IndexOf("var operation0 =", StringComparison.Ordinal);
        var secondOperationStart = generated.IndexOf("var operation1 =", StringComparison.Ordinal);
        Assert.True(firstOperationStart >= 0 && secondOperationStart > firstOperationStart);
        var firstOperation = generated[firstOperationStart..secondOperationStart];
        var secondOperation = generated[secondOperationStart..];
        Assert.Contains("WithSummary<global::Ling.RemoteServices.AspNetCore.RemoteServiceOperationConventionBuilder>(operation0, \"GET only\")", firstOperation);
        Assert.DoesNotContain("GET only", secondOperation);
        Assert.Contains("AuthorizationPolicyNames = new string?[] { \"GetPolicy\" }", firstOperation);
        Assert.DoesNotContain("GetPolicy", secondOperation);
        Assert.Contains("operation1.WithRequestTimeout(global::System.TimeSpan.FromMilliseconds(250))", secondOperation);
        Assert.DoesNotContain("FromMilliseconds(250)", firstOperation);
        Assert.DoesNotContain("ProducesResponseTypeMetadata(422", firstOperation);
        Assert.Contains("ProducesResponseTypeMetadata(422, typeof(global::Microsoft.AspNetCore.Mvc.ProblemDetails)", secondOperation);
    }

#if !NET10_0_OR_GREATER
    [Fact]
    public void Generator_reports_dotnet10_endpoint_features_for_older_target_frameworks()
    {
        const string source = """
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            namespace GeneratorFixtures;

            [RemoteService("/api/net10")]
            public interface INet10FeaturesService
            {
                [Post]
                [RemoteDisableValidation]
                [RemoteAllowCookieRedirect]
                Task<string> ExecuteAsync();
            }
            """;

        var run = CSharpSourceGeneratorVerifier<MinimalApiGenerator>.Run(
            source,
            MetadataReference.CreateFromFile(typeof(RemoteServiceEndpointConventionRegistry).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(OpenApiRouteHandlerBuilderExtensions).Assembly.Location));

        AssertNoCompilerErrors(run.OutputCompilation);
        Assert.Equal(2, run.GeneratorResult.Diagnostics.Count(diagnostic => diagnostic.Id == "LRS008"));
        Assert.DoesNotContain(
            run.GeneratorResult.GeneratedSources.Select(item => item.SourceText.ToString()),
            generated => generated.Contains("DisableValidation<", StringComparison.Ordinal)
                || generated.Contains("AllowCookieRedirect<", StringComparison.Ordinal));
    }
#endif

#if NET10_0_OR_GREATER
    [Fact]
    public void Generator_emits_and_compiles_dotnet10_endpoint_features()
    {
        const string source = """
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            namespace GeneratorFixtures;

            [RemoteService("/api/net10")]
            public interface INet10FeaturesService
            {
                [Post]
                [RemoteDisableValidation]
                [RemoteAllowCookieRedirect]
                Task<string> ExecuteAsync();
            }
            """;

        var run = CSharpSourceGeneratorVerifier<MinimalApiGenerator>.Run(
            source,
            MetadataReference.CreateFromFile(typeof(RemoteServiceEndpointConventionRegistry).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(OpenApiRouteHandlerBuilderExtensions).Assembly.Location));
        var generated = Assert.Single(
                run.GeneratorResult.GeneratedSources,
                item => item.HintName.EndsWith(".Endpoints.g.cs", StringComparison.Ordinal))
            .SourceText
            .ToString();

        AssertNoCompilerErrors(run.OutputCompilation);
        Assert.DoesNotContain(run.GeneratorResult.Diagnostics, diagnostic => diagnostic.Id == "LRS008");
        Assert.Contains("ValidationEndpointConventionBuilderExtensions.DisableValidation<", generated);
        Assert.Contains("CookieRedirectEndpointConventionBuilderExtensions.AllowCookieRedirect<", generated);
    }
#endif

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
