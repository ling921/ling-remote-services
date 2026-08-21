using Microsoft.CodeAnalysis;

namespace Ling.RemoteServices.Generators;

internal sealed record ParameterModel(IParameterSymbol Symbol, BindKind Kind, string Name);
