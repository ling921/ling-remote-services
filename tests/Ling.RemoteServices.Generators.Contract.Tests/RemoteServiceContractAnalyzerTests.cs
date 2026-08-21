using Ling.RemoteServices.Generators.Testing;
using Microsoft.CodeAnalysis;

namespace Ling.RemoteServices.Generators.Contract.Tests;

public class RemoteServiceContractAnalyzerTests
{
    [Fact]
    public async Task Analyzer_accepts_a_valid_contract()
    {
        var diagnostics = await CSharpAnalyzerVerifier<RemoteServiceContractAnalyzer>
            .GetDiagnosticsAsync(GeneratorTestSources.Contracts);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task LRS001_reports_synchronous_remote_methods()
    {
        const string source = """
            using Ling.RemoteServices.Attributes;

            [RemoteService("/api/test")]
            public interface ITestService
            {
                [Get]
                string Get();
            }
            """;

        var diagnostic = await GetSingleDiagnosticAsync(source, "LRS001");

        Assert.Contains("must return Task", diagnostic.GetMessage());
    }

    [Fact]
    public async Task LRS002_reports_remote_methods_without_an_http_method()
    {
        const string source = """
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            [RemoteService("/api/test")]
            public interface ITestService
            {
                Task<string> GetAsync();
            }
            """;

        var diagnostic = await GetSingleDiagnosticAsync(source, "LRS002");

        Assert.Contains("at least one", diagnostic.GetMessage());
    }

    [Theory]
    [MemberData(nameof(InvalidContractSources))]
    public async Task LRS003_reports_unsupported_contracts(
        string source,
        string expectedMessage)
    {
        var diagnostic = await GetSingleDiagnosticAsync(source, "LRS003");

        Assert.Contains(expectedMessage, diagnostic.GetMessage());
    }

    [Fact]
    public async Task LRS005_requires_a_client_default_for_multiple_http_methods()
    {
        const string source = """
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            [RemoteService("/api/test")]
            public interface ITestService
            {
                [Get, Post]
                Task<string> GetAsync();
            }
            """;

        var diagnostic = await GetSingleDiagnosticAsync(source, "LRS005");

        Assert.Contains("IsClientDefault = true", diagnostic.GetMessage());
    }

    [Fact]
    public async Task LRS006_rejects_multiple_client_defaults()
    {
        const string source = """
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            [RemoteService("/api/test")]
            public interface ITestService
            {
                [Get(IsClientDefault = true), Post(IsClientDefault = true)]
                Task<string> GetAsync();
            }
            """;

        var diagnostic = await GetSingleDiagnosticAsync(source, "LRS006");

        Assert.Contains("must not mark more than one", diagnostic.GetMessage());
    }

    [Fact]
    public async Task LRS007_rejects_duplicate_http_operations()
    {
        const string source = """
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            [RemoteService("/api/test")]
            public interface ITestService
            {
                [Get("item")]
                Task<string> GetFirstAsync();

                [Get("item")]
                Task<string> GetSecondAsync();
            }
            """;

        var diagnostic = await GetSingleDiagnosticAsync(source, "LRS007");

        Assert.Contains("GET /api/test/item", diagnostic.GetMessage());
    }

    public static TheoryData<string, string> InvalidContractSources => new()
    {
        {
            """
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            [RemoteService("/api/test")]
            internal interface ITestService
            {
                [Get]
                Task GetAsync();
            }
            """,
            "public, non-generic interface"
        },
        {
            """
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            [RemoteService("/api/test")]
            public interface ITestService<T>
            {
                [Get]
                Task GetAsync();
            }
            """,
            "public, non-generic interface"
        },
        {
            """
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            [RemoteService("/api/test")]
            public interface ITestService
            {
                [Get("by-id")]
                Task<string> GetAsync(int id);

                [Get("by-name")]
                Task<string> GetAsync(string name);
            }
            """,
            "cannot overload method"
        },
        {
            """
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            [RemoteService("/api/test")]
            public interface ITestService
            {
                [Get]
                Task<T> GetAsync<T>();
            }
            """,
            "cannot be generic"
        },
        {
            """
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            [RemoteService("/api/test")]
            public interface ITestService
            {
                [Get]
                Task GetAsync(ref int value);
            }
            """,
            "ref/out parameters"
        },
        {
            """
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            [RemoteService("/api/{id=1}")]
            public interface ITestService
            {
                [Get]
                Task GetAsync(int id);
            }
            """,
            "default route values are not supported"
        },
        {
            """
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            [RemoteService("/api/{id:custom}")]
            public interface ITestService
            {
                [Get]
                Task GetAsync(int id);
            }
            """,
            "custom route policy 'custom' is not supported"
        },
        {
            """
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            [RemoteService("/api/{id")]
            public interface ITestService
            {
                [Get]
                Task GetAsync(int id);
            }
            """,
            "unbalanced braces"
        },
        {
            """
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            [RemoteService("/api/test")]
            public interface ITestService
            {
                [Get]
                Task GetAsync([Body] object request);
            }
            """,
            "invalid request body"
        },
        {
            """
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            [RemoteService("/api/test")]
            public interface ITestService
            {
                [Post]
                Task GetAsync([Body] object first, [Body] object second);
            }
            """,
            "invalid request body"
        },
        {
            """
            using Ling.RemoteServices.Attributes;
            using System.Threading.Tasks;

            [RemoteService("/api/test")]
            [RemoteCors("")]
            public interface ITestService
            {
                [Get]
                Task GetAsync();
            }
            """,
            "requires a non-empty policy name"
        }
    };

    private static async Task<Diagnostic> GetSingleDiagnosticAsync(
        string source,
        string diagnosticId)
    {
        var diagnostics = await CSharpAnalyzerVerifier<RemoteServiceContractAnalyzer>
            .GetDiagnosticsAsync(source);
        var diagnostic = Assert.Single(diagnostics, value => value.Id == diagnosticId);

        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.True(diagnostic.Location.IsInSource);
        return diagnostic;
    }
}
