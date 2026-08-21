using Microsoft.CodeAnalysis;

namespace Ling.RemoteServices.Generators;

internal static class ContractDiagnostics
{
    public static readonly DiagnosticDescriptor AsyncRequired = new(
        "LRS001",
        "Remote method must be asynchronous",
        "Remote method '{0}' must return Task, Task<T>, ValueTask, or ValueTask<T>",
        "Ling.RemoteServices",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingHttpMethod = new(
        "LRS002",
        "Remote method requires an HTTP method",
        "Remote method '{0}' must have at least one Get/Post/Put/Patch/Delete attribute",
        "Ling.RemoteServices",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Invalid = new(
        "LRS003",
        "Unsupported remote contract",
        "{0}",
        "Ling.RemoteServices",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoContracts = new(
        "LRS004",
        "No remote service contracts were found",
        "Remote service {0} generation is enabled, but no contract was found in this project or its direct references",
        "Ling.RemoteServices",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ClientDefaultRequired = new(
        "LRS005",
        "Client default HTTP method is required",
        "Remote method '{0}' exposes multiple HTTP methods and must mark exactly one as IsClientDefault = true",
        "Ling.RemoteServices",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MultipleClientDefaults = new(
        "LRS006",
        "Remote method has multiple client defaults",
        "Remote method '{0}' must not mark more than one HTTP method as IsClientDefault = true",
        "Ling.RemoteServices",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateHttpOperation = new(
        "LRS007",
        "Duplicate remote HTTP operation",
        "Remote HTTP operation '{0} {1}' is declared more than once in service '{2}'",
        "Ling.RemoteServices",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
