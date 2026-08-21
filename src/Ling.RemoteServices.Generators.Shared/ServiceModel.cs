using Microsoft.CodeAnalysis;

namespace Ling.RemoteServices.Generators;

internal sealed record ServiceModel(
    INamedTypeSymbol Symbol,
    string RoutePrefix,
    List<MethodModel> Methods);
