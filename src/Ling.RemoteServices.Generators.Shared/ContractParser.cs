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
        var methods = new List<MethodModel>();
        var hasInvalidMethod = false;
        var remoteMethods = service.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(symbol => symbol.MethodKind == MethodKind.Ordinary)
            .ToArray();
        var overloadedMethod = remoteMethods
            .GroupBy(method => method.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (overloadedMethod is not null)
        {
            ReportInvalid(
                reportDiagnostic,
                overloadedMethod.First(),
                $"Remote service '{service.Name}' cannot overload method "
                + $"'{overloadedMethod.Key}'. Use distinct operation method names.");
            return null;
        }

        foreach (var method in remoteMethods)
        {
            var model = ParseMethod(method, routePrefix, reportDiagnostic);
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

        return new ServiceModel(service, routePrefix, methods);
    }

    private static MethodModel? ParseMethod(
        IMethodSymbol method,
        string routePrefix,
        Action<Diagnostic>? reportDiagnostic)
    {
        var verbAttributes = method.GetAttributes()
            .Where(attribute => GetVerb(attribute.AttributeClass?.Name) is not null)
            .ToArray();

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
            var operation = ParseOperation(method, routePrefix, verbAttribute, reportDiagnostic);
            if (operation is null)
            {
                return null;
            }

            operations.Add(operation);
        }

        var clientDefault = operations.Count == 1
            ? operations[0]
            : operations.Single(operation => operation.IsClientDefault);

        return new MethodModel(
            method,
            resultType,
            GetSummary(method),
            EndpointPolicyParser.ParseEffective(
                method.ContainingType,
                method,
                reportDiagnostic),
            operations,
            clientDefault);
    }

    private static HttpOperationModel? ParseOperation(
        IMethodSymbol method,
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
            .Select(parameter => ParseParameter(parameter, verb, routeNames))
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

        return new HttpOperationModel(
            verb,
            relativeRoute,
            fullRoute,
            parameters,
            successStatus,
            responseContentType,
            IsClientDefault(verbAttribute));
    }

    private static bool IsClientDefault(AttributeData attribute)
    {
        return attribute.NamedArguments
            .FirstOrDefault(argument => argument.Key == "IsClientDefault")
            .Value.Value is true;
    }

    private static ParameterModel ParseParameter(
        IParameterSymbol parameter,
        string verb,
        HashSet<string> routeNames)
    {
        if (parameter.Type.ToDisplayString() == "System.Threading.CancellationToken")
        {
            return new ParameterModel(parameter, BindKind.Cancellation, parameter.Name);
        }

        var bindingAttribute = parameter.GetAttributes().FirstOrDefault(attribute =>
            attribute.AttributeClass?.ContainingNamespace.ToDisplayString()
                == "Ling.RemoteServices.Attributes");
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
