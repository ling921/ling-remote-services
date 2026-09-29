using Microsoft.CodeAnalysis;

namespace Ling.RemoteServices.Generators;

internal sealed record MethodModel(
    IMethodSymbol Symbol,
    ITypeSymbol? Result,
    string? Summary,
    List<HttpOperationModel> Operations,
    HttpOperationModel ClientDefaultOperation,
    IReadOnlyList<IMethodSymbol> Declarations,
    string? EndpointName);
