using Microsoft.CodeAnalysis;
using System.Net;
using System.Text.RegularExpressions;

namespace Ling.RemoteServices.Generators;

internal static class ContractParser
{
    private static readonly string[] SupportedRoutePolicies =
    [
        "int", "bool", "datetime", "decimal", "double", "float", "guid", "long",
        "minlength", "maxlength", "length", "min", "max", "range", "alpha", "regex", "required"
    ];

    public static ServiceModel? Parse(
        INamedTypeSymbol service,
        Action<Diagnostic>? reportDiagnostic = null)
    {
        if (service.DeclaredAccessibility != Accessibility.Public || service.IsGenericType)
        {
            ReportInvalid(
                reportDiagnostic,
                service,
                $"Remote service '{service.Name}' must be a public, non-generic interface.");
            return null;
        }

        var serviceAttribute = service.GetAttributes().FirstOrDefault(attribute =>
            attribute.AttributeClass?.ToDisplayString() == ContractNames.ServiceAttribute);
        if (serviceAttribute is null)
        {
            return null;
        }

        var declaredServiceRoute = serviceAttribute.ConstructorArguments.FirstOrDefault().Value as string
            ?? string.Empty;
        var routePrefix = NormalizeRoute(declaredServiceRoute);
        var configurationSources = GetInterfaceHierarchy(service);
        var methods = new List<MethodModel>();
        var hasInvalidMethod = false;
        var methodGroups = configurationSources
            .Append(service)
            .SelectMany(type => type.GetMembers().OfType<IMethodSymbol>())
            .Where(symbol => symbol.MethodKind == MethodKind.Ordinary)
            .GroupBy(method => method.Name, StringComparer.Ordinal)
            .ToArray();
        var overloadedMethod = methodGroups.FirstOrDefault(group =>
            group.Select(GetMethodSignature).Distinct(StringComparer.Ordinal).Count() > 1);

        if (overloadedMethod is not null)
        {
            ReportInvalid(
                reportDiagnostic,
                overloadedMethod.First(),
                $"Remote service '{service.Name}' cannot overload method "
                + $"'{overloadedMethod.Key}'. Use distinct operation method names.");
            return null;
        }

        foreach (var methodGroup in methodGroups)
        {
            var declarations = methodGroup.ToArray();
            var method = declarations[declarations.Length - 1];
            if (!ValidateInheritedOperationDeclarations(declarations, reportDiagnostic))
            {
                hasInvalidMethod = true;
                continue;
            }

            var model = ParseMethod(
                method,
                declarations,
                configurationSources,
                service,
                routePrefix,
                reportDiagnostic);
            if (model is not null)
            {
                methods.Add(model);
            }
            else
            {
                hasInvalidMethod = true;
            }
        }

        if (hasInvalidMethod)
        {
            return null;
        }

        var duplicateOperation = methods
            .SelectMany(method => method.Operations.Select(operation => (Method: method, Operation: operation)))
            .GroupBy(
                item => item.Operation.Verb + "\n" + item.Operation.FullRoute,
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateOperation is not null)
        {
            var duplicate = duplicateOperation.First();
            reportDiagnostic?.Invoke(Diagnostic.Create(
                ContractDiagnostics.DuplicateHttpOperation,
                duplicate.Method.Symbol.Locations.FirstOrDefault(),
                duplicate.Operation.Verb,
                duplicate.Operation.FullRoute,
                service.Name));
            return null;
        }

        var duplicateEndpointName = methods
            .Where(method => method.EndpointName is not null)
            .GroupBy(method => method.EndpointName!, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateEndpointName is not null)
        {
            ReportInvalid(
                reportDiagnostic,
                duplicateEndpointName.Last().Symbol,
                $"Remote endpoint name '{duplicateEndpointName.Key}' is used more than once in service '{service.Name}'.");
            return null;
        }

        return new ServiceModel(service, routePrefix, methods, configurationSources);
    }

    private static MethodModel? ParseMethod(
        IMethodSymbol method,
        IReadOnlyList<IMethodSymbol> declarations,
        IReadOnlyList<INamedTypeSymbol> configurationSources,
        INamedTypeSymbol service,
        string routePrefix,
        Action<Diagnostic>? reportDiagnostic)
    {
        var verbDeclaration = declarations.LastOrDefault(declaration => declaration.GetAttributes()
            .Any(attribute => GetVerb(attribute.AttributeClass?.Name) is not null));
        var verbAttributes = verbDeclaration?.GetAttributes()
            .Where(attribute => GetVerb(attribute.AttributeClass?.Name) is not null)
            .ToArray()
            ?? [];

        if (verbAttributes.Length == 0)
        {
            reportDiagnostic?.Invoke(Diagnostic.Create(
                ContractDiagnostics.MissingHttpMethod,
                method.Locations.FirstOrDefault(),
                method.Name));
            return null;
        }

        if (!TryGetResultType(method.ReturnType, out var resultType))
        {
            reportDiagnostic?.Invoke(Diagnostic.Create(
                ContractDiagnostics.AsyncRequired,
                method.Locations.FirstOrDefault(),
                method.Name));
            return null;
        }

        if (method.IsGenericMethod || method.Parameters.Any(parameter => parameter.RefKind != RefKind.None))
        {
            ReportInvalid(
                reportDiagnostic,
                method,
                $"Remote method '{method.Name}' cannot be generic or contain ref/out parameters.");
            return null;
        }

        var defaultAttributes = verbAttributes
            .Where(IsClientDefault)
            .ToArray();
        if (verbAttributes.Length > 1 && defaultAttributes.Length == 0)
        {
            reportDiagnostic?.Invoke(Diagnostic.Create(
                ContractDiagnostics.ClientDefaultRequired,
                method.Locations.FirstOrDefault(),
                method.Name));
            return null;
        }

        if (defaultAttributes.Length > 1)
        {
            reportDiagnostic?.Invoke(Diagnostic.Create(
                ContractDiagnostics.MultipleClientDefaults,
                method.Locations.FirstOrDefault(),
                method.Name));
            return null;
        }

        var operations = new List<HttpOperationModel>();
        foreach (var verbAttribute in verbAttributes)
        {
            var operation = ParseOperation(method, declarations, routePrefix, verbAttribute, reportDiagnostic);
            if (operation is null)
            {
                return null;
            }

            operations.Add(operation);
        }

        var clientDefault = operations.Count == 1
            ? operations[0]
            : operations.Single(operation => operation.IsClientDefault);

        var declaredMetadata = EndpointMetadataParser.Parse(
            configurationSources.Cast<ISymbol>().Append(service).Concat(declarations),
            reportDiagnostic);
        var endpointName = declaredMetadata
            .LastOrDefault(item => item.Kind == EndpointMetadataKind.EndpointName)
            ?.Text;
        if (endpointName is not null && operations.Count != 1)
        {
            ReportInvalid(
                reportDiagnostic,
                method,
                $"Remote endpoint name on '{method.Name}' requires exactly one HTTP operation.");
            return null;
        }

        if (endpointName is not null && string.IsNullOrWhiteSpace(endpointName))
        {
            ReportInvalid(reportDiagnostic, method, $"Remote endpoint name on '{method.Name}' cannot be empty.");
            return null;
        }

        for (var index = 0; index < operations.Count; index++)
        {
            var operation = operations[index];
            var httpMethod = GetRemoteHttpMethodValue(operation.Verb);
            var operationMetadata = declaredMetadata
                .Where(item => !item.SelectsHttpMethod || item.HttpMethod == httpMethod)
                .ToArray();
            operations[index] = operation with
            {
                EndpointMetadata = operationMetadata,
                EndpointPolicies = EndpointPolicyParser.ParseEffective(
                    configurationSources.Cast<ISymbol>().Append(service).ToArray(),
                    declarations,
                    httpMethod,
                    reportDiagnostic)
            };
        }

        return new MethodModel(
            method,
            resultType,
            declarations.Select(GetSummary).LastOrDefault(summary => summary is not null),
            operations,
            clientDefault,
            declarations,
            endpointName);
    }

    private static HttpOperationModel? ParseOperation(
        IMethodSymbol method,
        IReadOnlyList<IMethodSymbol> declarations,
        string routePrefix,
        AttributeData verbAttribute,
        Action<Diagnostic>? reportDiagnostic)
    {
        var verb = GetVerb(verbAttribute.AttributeClass!.Name)!;
        var declaredOperationRoute = verbAttribute.ConstructorArguments.FirstOrDefault().Value as string
            ?? string.Empty;
        var relativeRoute = NormalizeRoute(declaredOperationRoute);
        var fullRoute = CombineRoutes(routePrefix, relativeRoute);

        if (!ValidateRoute(fullRoute, out var routeError))
        {
            ReportInvalid(
                reportDiagnostic,
                method,
                $"Remote method '{method.Name}' has an unsupported route: {routeError}");
            return null;
        }

        var routeNames = GetRouteNames(fullRoute);
        var parameters = method.Parameters
            .Select((parameter, index) => ParseParameter(
                parameter,
                FindInheritedBindingAttribute(declarations, index),
                verb,
                routeNames))
            .ToList();

        var hasInvalidBody = parameters.Count(parameter => parameter.Kind == BindKind.Body) > 1
            || (verb == "GET" && parameters.Any(parameter => parameter.Kind == BindKind.Body));
        if (hasInvalidBody)
        {
            ReportInvalid(
                reportDiagnostic,
                method,
                $"Remote method '{method.Name}' has an invalid request body.");
            return null;
        }

        var successStatus = verbAttribute.NamedArguments
            .FirstOrDefault(argument => argument.Key == "SuccessStatusCode")
            .Value.Value as int?;
        var responseContentType = verbAttribute.NamedArguments
            .FirstOrDefault(argument => argument.Key == "ResponseContentType")
            .Value.Value as string;

        if (verbAttribute.NamedArguments.Any(argument => argument.Key == "SuccessStatusCode")
            && successStatus is < 100 or > 299)
        {
            ReportInvalid(
                reportDiagnostic,
                method,
                $"Success status code on '{method.Name}' must be from 100 through 299.");
            return null;
        }

        return new HttpOperationModel(
            verb,
            relativeRoute,
            fullRoute,
            parameters,
            successStatus,
            responseContentType,
            IsClientDefault(verbAttribute),
            Array.Empty<EndpointMetadataModel>(),
            EndpointPolicyModel.Empty);
    }

    private static List<INamedTypeSymbol> GetInterfaceHierarchy(INamedTypeSymbol service)
    {
        var result = new List<INamedTypeSymbol>();
        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        void Visit(INamedTypeSymbol current)
        {
            if (!visited.Add(current))
            {
                return;
            }

            foreach (var baseInterface in current.Interfaces.OrderBy(type => type.ToDisplayString(), StringComparer.Ordinal))
            {
                Visit(baseInterface);
            }

            if (!SymbolEqualityComparer.Default.Equals(current, service))
            {
                result.Add(current);
            }
        }

        Visit(service);
        return result;
    }

    private static int GetRemoteHttpMethodValue(string verb) => verb switch
    {
        "GET" => 0,
        "POST" => 1,
        "PUT" => 2,
        "PATCH" => 3,
        "DELETE" => 4,
        _ => throw new InvalidOperationException($"Unsupported HTTP method '{verb}'.")
    };

    private static string GetMethodSignature(IMethodSymbol method) =>
        method.Name + "(" + string.Join(",", method.Parameters.Select(parameter =>
            parameter.RefKind + ":" + parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
        + ")->" + method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static bool ValidateInheritedOperationDeclarations(
        IReadOnlyList<IMethodSymbol> declarations,
        Action<Diagnostic>? reportDiagnostic)
    {
        var declarationsWithVerbs = declarations
            .Where(declaration => declaration.GetAttributes().Any(attribute => GetVerb(attribute.AttributeClass?.Name) is not null))
            .ToArray();
        if (declarationsWithVerbs.Length < 2)
        {
            return true;
        }

        var mostSpecificDeclarations = declarationsWithVerbs
            .Where(declaration => !declarationsWithVerbs.Any(other =>
                !SymbolEqualityComparer.Default.Equals(declaration.ContainingType, other.ContainingType)
                && other.ContainingType.AllInterfaces.Any(baseInterface =>
                    SymbolEqualityComparer.Default.Equals(baseInterface, declaration.ContainingType))))
            .ToArray();
        if (mostSpecificDeclarations.Length < 2)
        {
            return true;
        }

        var signatures = mostSpecificDeclarations
            .Select(declaration => string.Join(";", declaration.GetAttributes()
                .Where(attribute => GetVerb(attribute.AttributeClass?.Name) is not null)
                .Select(attribute => attribute.AttributeClass!.ToDisplayString()
                    + "(" + string.Join(",", attribute.ConstructorArguments.Select(argument => argument.Value?.ToString())) + ")"
                    + "{" + string.Join(",", attribute.NamedArguments.Select(argument => argument.Key + "=" + argument.Value.Value)) + "}")))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (signatures.Length == 1)
        {
            return true;
        }

        ReportInvalid(
            reportDiagnostic,
            mostSpecificDeclarations[0],
            $"Inherited declarations of remote method '{mostSpecificDeclarations[0].Name}' define conflicting HTTP operations. Declare the method on the service interface to resolve the conflict.");
        return false;
    }

    private static bool IsClientDefault(AttributeData attribute)
    {
        return attribute.NamedArguments
            .FirstOrDefault(argument => argument.Key == "IsClientDefault")
            .Value.Value is true;
    }

    private static ParameterModel ParseParameter(
        IParameterSymbol parameter,
        AttributeData? inheritedBinding,
        string verb,
        HashSet<string> routeNames)
    {
        if (parameter.Type.ToDisplayString() == "System.Threading.CancellationToken")
        {
            return new ParameterModel(parameter, BindKind.Cancellation, parameter.Name);
        }

        var bindingAttribute = parameter.GetAttributes().FirstOrDefault(attribute =>
            attribute.AttributeClass?.ContainingNamespace.ToDisplayString()
                == "Ling.RemoteServices.Attributes") ?? inheritedBinding;
        var name = GetBindingName(bindingAttribute) ?? parameter.Name;

        var kind = bindingAttribute?.AttributeClass?.Name switch
        {
            "PathAttribute" => BindKind.Path,
            "QueryAttribute" => BindKind.Query,
            "HeaderAttribute" => BindKind.Header,
            "BodyAttribute" => BindKind.Body,
            "FormAttribute" => BindKind.Form,
            _ when routeNames.Contains(parameter.Name) => BindKind.Path,
            _ when verb is "POST" or "PUT" or "PATCH" && !IsScalar(parameter.Type) => BindKind.Body,
            _ => BindKind.Query
        };

        return new ParameterModel(parameter, kind, name);
    }

    private static AttributeData? FindInheritedBindingAttribute(
        IReadOnlyList<IMethodSymbol> declarations,
        int parameterIndex)
    {
        for (var declarationIndex = declarations.Count - 1; declarationIndex >= 0; declarationIndex--)
        {
            var declaration = declarations[declarationIndex];
            if (declaration.Parameters.Length <= parameterIndex)
            {
                continue;
            }

            var attribute = declaration.Parameters[parameterIndex].GetAttributes().FirstOrDefault(candidate =>
                candidate.AttributeClass?.ContainingNamespace.ToDisplayString()
                    == "Ling.RemoteServices.Attributes");
            if (attribute is not null)
            {
                return attribute;
            }
        }

        return null;
    }

    private static string? GetBindingName(AttributeData? attribute)
    {
        if (attribute is null)
        {
            return null;
        }

        var constructorName = attribute.ConstructorArguments.FirstOrDefault().Value as string;
        if (!string.IsNullOrEmpty(constructorName))
        {
            return constructorName;
        }

        return attribute.NamedArguments
            .FirstOrDefault(argument => argument.Key == "Name")
            .Value.Value as string;
    }

    private static bool TryGetResultType(ITypeSymbol type, out ITypeSymbol? resultType)
    {
        resultType = null;
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        var definition = namedType.ConstructedFrom.ToDisplayString();
        if (definition is "System.Threading.Tasks.Task" or "System.Threading.Tasks.ValueTask")
        {
            return true;
        }

        if (definition is "System.Threading.Tasks.Task<TResult>"
            or "System.Threading.Tasks.ValueTask<TResult>")
        {
            resultType = namedType.TypeArguments[0];
            return true;
        }

        return false;
    }

    private static string? GetVerb(string? attributeName)
    {
        return attributeName switch
        {
            "GetAttribute" => "GET",
            "PostAttribute" => "POST",
            "PutAttribute" => "PUT",
            "PatchAttribute" => "PATCH",
            "DeleteAttribute" => "DELETE",
            _ => null
        };
    }

    private static string NormalizeRoute(string route)
    {
        var value = route.Trim('/');
        return value.Length == 0 ? string.Empty : "/" + value;
    }

    private static string CombineRoutes(string routePrefix, string relativeRoute)
    {
        return routePrefix + relativeRoute;
    }

    private static HashSet<string> GetRouteNames(string route)
    {
        var names = Regex.Matches(route, @"\{\*{0,2}(?<name>[A-Za-z_][A-Za-z0-9_]*)")
            .Cast<Match>()
            .Select(match => match.Groups["name"].Value);
        return new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
    }

    private static bool ValidateRoute(string route, out string error)
    {
        error = string.Empty;
        if (route.Contains('='))
        {
            error = "default route values are not supported";
            return false;
        }

        var openingBraces = route.Count(character => character == '{');
        var closingBraces = route.Count(character => character == '}');
        if (openingBraces != closingBraces)
        {
            error = "unbalanced braces";
            return false;
        }

        foreach (Match match in Regex.Matches(route, @":(?<policy>[A-Za-z][A-Za-z0-9]*)(?:\([^}]*\))?"))
        {
            var policy = match.Groups["policy"].Value;
            if (!SupportedRoutePolicies.Contains(policy, StringComparer.OrdinalIgnoreCase))
            {
                error = $"custom route policy '{policy}' is not supported";
                return false;
            }
        }

        return true;
    }

    private static string? GetSummary(IMethodSymbol method)
    {
        var xml = method.GetDocumentationCommentXml();
        if (!string.IsNullOrWhiteSpace(xml))
        {
            var match = Regex.Match(xml, "<summary>(?<text>[\\s\\S]*?)</summary>");
            if (match.Success)
            {
                var decodedText = WebUtility.HtmlDecode(match.Groups["text"].Value);
                return Regex.Replace(decodedText, "<[^>]+>", string.Empty).Trim();
            }
        }

        foreach (var attribute in method.ContainingAssembly.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString()
                    != ContractNames.MethodDocumentationAttribute
                || attribute.ConstructorArguments.Length != 3
                || attribute.ConstructorArguments[0].Value is not INamedTypeSymbol serviceType
                || !SymbolEqualityComparer.Default.Equals(serviceType, method.ContainingType)
                || attribute.ConstructorArguments[1].Value as string != method.Name)
            {
                continue;
            }

            return attribute.ConstructorArguments[2].Value as string;
        }

        return null;
    }

    private static void ReportInvalid(
        Action<Diagnostic>? reportDiagnostic,
        ISymbol symbol,
        string message)
    {
        reportDiagnostic?.Invoke(Diagnostic.Create(
            ContractDiagnostics.Invalid,
            symbol.Locations.FirstOrDefault(),
            message));
    }

    internal static bool IsScalar(ITypeSymbol type)
    {
        var displayName = type.ToDisplayString().TrimEnd('?');
        return type.SpecialType != SpecialType.None
            || type.TypeKind == TypeKind.Enum
            || displayName is "System.Guid"
                or "System.DateTime"
                or "System.DateTimeOffset"
                or "System.DateOnly"
                or "System.TimeOnly"
                or "System.TimeSpan"
                or "System.Uri";
    }
}
