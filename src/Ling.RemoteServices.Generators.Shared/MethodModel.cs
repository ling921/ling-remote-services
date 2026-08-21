using Microsoft.CodeAnalysis;

namespace Ling.RemoteServices.Generators;

internal sealed record MethodModel(
    IMethodSymbol Symbol,
    ITypeSymbol? Result,
    string? Summary,
    EndpointPolicyModel EndpointPolicies,
    List<HttpOperationModel> Operations,
    HttpOperationModel ClientDefaultOperation);
