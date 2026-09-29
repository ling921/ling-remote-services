using Microsoft.CodeAnalysis;

namespace Ling.RemoteServices.Generators;

internal static class EndpointMetadataParser
{
    private const string Prefix = "Ling.RemoteServices.Attributes.";

    public static IReadOnlyList<EndpointMetadataModel> Parse(
        IEnumerable<ISymbol> symbols,
        Action<Diagnostic>? reportDiagnostic)
    {
        var result = new List<EndpointMetadataModel>();
        foreach (var symbol in symbols)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                var model = Parse(attribute, symbol, reportDiagnostic);
                if (model is not null)
                {
                    result.Add(model);
                }
            }
        }

        return result;
    }

    private static EndpointMetadataModel? Parse(
        AttributeData attribute,
        ISymbol symbol,
        Action<Diagnostic>? reportDiagnostic)
    {
        var attributeType = attribute.AttributeClass;
        if (attributeType is null)
        {
            return null;
        }

        var typeName = attributeType.OriginalDefinition.ToDisplayString();
        var fullName = attributeType.ToDisplayString();
        EndpointMetadataKind kind;
        ITypeSymbol? valueType = null;
        int? statusCode = null;
        string? contentType = null;
        var contentTypes = new List<string>();
        var values = new List<string>();
        var namedValues = new Dictionary<string, object?>(StringComparer.Ordinal);
        string? text = null;
        int? number = null;
        bool? boolean = null;
        var isValid = true;

        switch (typeName)
        {
            case Prefix + "RemoteProducesAttribute<TResponse>":
                kind = EndpointMetadataKind.Produces;
                valueType = attributeType.TypeArguments[0];
                statusCode = GetInt(attribute, 0, 200);
                contentType = GetString(attribute, 1);
                AddStrings(contentTypes, GetArray(attribute, 2));
                break;
            case Prefix + "RemoteProducesAttribute":
                kind = EndpointMetadataKind.Produces;
                statusCode = GetInt(attribute, 0, 200);
                valueType = GetType(attribute, 1);
                contentType = GetString(attribute, 2);
                AddStrings(contentTypes, GetArray(attribute, 3));
                break;
            case Prefix + "RemoteProducesProblemAttribute":
            case Prefix + "RemoteProducesValidationProblemAttribute":
                kind = EndpointMetadataKind.Produces;
                var isProblemResponse = fullName.StartsWith(Prefix + "RemoteProducesProblemAttribute", StringComparison.Ordinal);
                statusCode = GetInt(attribute, 0, isProblemResponse ? 500 : 400);
                contentType = GetString(attribute, 1) ?? "application/problem+json";
                namedValues["IsProblem"] = isProblemResponse;
                namedValues["IsValidationProblem"] = !isProblemResponse;
                break;
            case Prefix + "RemoteAcceptsAttribute<TRequest>":
                kind = EndpointMetadataKind.Accepts;
                valueType = attributeType.TypeArguments[0];
                contentType = GetString(attribute, 0);
                boolean = GetBool(attribute, 1, false);
                AddStrings(contentTypes, GetArray(attribute, 2));
                break;
            case Prefix + "RemoteAcceptsAttribute":
                kind = EndpointMetadataKind.Accepts;
                valueType = GetType(attribute, 0);
                contentType = GetString(attribute, 1);
                boolean = GetBool(attribute, 2, false);
                AddStrings(contentTypes, GetArray(attribute, 3));
                break;
            case Prefix + "RemoteTagsAttribute":
                kind = EndpointMetadataKind.Tags;
                AddStrings(values, GetArray(attribute, 0));
                break;
            case Prefix + "RemoteSummaryAttribute":
                kind = EndpointMetadataKind.Summary;
                text = GetString(attribute, 0);
                break;
            case Prefix + "RemoteDescriptionAttribute":
                kind = EndpointMetadataKind.Description;
                text = GetString(attribute, 0);
                break;
            case Prefix + "RemoteExcludeFromDescriptionAttribute":
                kind = EndpointMetadataKind.ExcludeFromDescription;
                boolean = true;
                break;
            case Prefix + "RemoteHostAttribute":
                kind = EndpointMetadataKind.Host;
                AddStrings(values, GetArray(attribute, 0));
                break;
            case Prefix + "RemoteOrderAttribute":
                kind = EndpointMetadataKind.Order;
                number = GetInt(attribute, 0, 0);
                break;
            case Prefix + "RemoteDisplayNameAttribute":
                kind = EndpointMetadataKind.DisplayName;
                text = GetString(attribute, 0);
                break;
            case Prefix + "RemoteEndpointNameAttribute":
                kind = EndpointMetadataKind.EndpointName;
                text = GetString(attribute, 0);
                break;
            case Prefix + "RemoteShortCircuitAttribute":
                kind = EndpointMetadataKind.ShortCircuit;
                number = GetInt(attribute, 0, 0);
                if (number is int shortCircuitStatus
                    && shortCircuitStatus != 0
                    && (shortCircuitStatus is < 200 or > 599))
                {
                    ReportInvalid(symbol, attribute, $"Short-circuit status on '{symbol.Name}' must be from 200 through 599.", reportDiagnostic);
                    isValid = false;
                }

                break;
            case Prefix + "RemoteDisableAntiforgeryAttribute":
                kind = EndpointMetadataKind.DisableAntiforgery;
                boolean = true;
                break;
            case Prefix + "RemoteDisableValidationAttribute":
                kind = EndpointMetadataKind.DisableValidation;
                boolean = true;
                break;
            case Prefix + "RemoteAllowCookieRedirectAttribute":
                kind = EndpointMetadataKind.AllowCookieRedirect;
                boolean = true;
                break;
            case Prefix + "RemoteFormOptionsAttribute":
                kind = EndpointMetadataKind.FormOptions;
                foreach (var pair in attribute.NamedArguments)
                {
                    namedValues[pair.Key] = pair.Value.Value;
                }

                break;
            case Prefix + "RemoteFormMappingOptionsAttribute":
                kind = EndpointMetadataKind.FormMappingOptions;
                foreach (var pair in attribute.NamedArguments)
                {
                    namedValues[pair.Key] = pair.Value.Value;
                }

                break;
            default:
                return null;
        }

        var selector = attribute.NamedArguments.FirstOrDefault(argument => argument.Key == "HttpMethod");
        int? httpMethod = null;
        var selectsHttpMethod = selector.Key is not null;
        if (selectsHttpMethod && selector.Value.Value is int methodValue)
        {
            if (methodValue is < 0 or > 4)
            {
                ReportInvalid(symbol, attribute, $"Endpoint metadata on '{symbol.Name}' has an invalid HTTP method selector.", reportDiagnostic);
            }
            else
            {
                httpMethod = methodValue;
            }
        }

        if (kind == EndpointMetadataKind.Produces
            && statusCode is int responseStatus
            && (responseStatus is < 100 or > 599))
        {
            ReportInvalid(symbol, attribute, $"Response metadata on '{symbol.Name}' must use an HTTP status from 100 through 599.", reportDiagnostic);
            isValid = false;
        }

        if (kind == EndpointMetadataKind.Accepts && string.IsNullOrWhiteSpace(contentType))
        {
            ReportInvalid(symbol, attribute, $"Request metadata on '{symbol.Name}' requires a content type.", reportDiagnostic);
            isValid = false;
        }

        if (selectsHttpMethod && (selector.Value.Value is not int selectedMethod || selectedMethod is < 0 or > 4))
        {
            isValid = false;
        }

        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key != "HttpMethod")
            {
                namedValues[argument.Key] = argument.Value.Value;
            }
        }

        if (!isValid)
        {
            return null;
        }

        return new EndpointMetadataModel(
            kind,
            valueType,
            statusCode,
            contentType,
            contentTypes,
            values,
            namedValues,
            text,
            number,
            boolean,
            httpMethod,
            selectsHttpMethod);
    }

    private static int GetInt(AttributeData attribute, int index, int defaultValue) =>
        attribute.ConstructorArguments.Length > index && attribute.ConstructorArguments[index].Value is int value
            ? value
            : defaultValue;

    private static bool GetBool(AttributeData attribute, int index, bool defaultValue) =>
        attribute.ConstructorArguments.Length > index && attribute.ConstructorArguments[index].Value is bool value
            ? value
            : defaultValue;

    private static string? GetString(AttributeData attribute, int index) =>
        attribute.ConstructorArguments.Length > index ? attribute.ConstructorArguments[index].Value as string : null;

    private static ITypeSymbol? GetType(AttributeData attribute, int index) =>
        attribute.ConstructorArguments.Length > index ? attribute.ConstructorArguments[index].Value as ITypeSymbol : null;

    private static IReadOnlyList<TypedConstant> GetArray(AttributeData attribute, int index) =>
        attribute.ConstructorArguments.Length > index ? attribute.ConstructorArguments[index].Values : [];

    private static void AddStrings(ICollection<string> destination, IEnumerable<TypedConstant> values)
    {
        foreach (var value in values)
        {
            if (value.Value is string text)
            {
                destination.Add(text);
            }
        }
    }

    private static void ReportInvalid(
        ISymbol symbol,
        AttributeData attribute,
        string message,
        Action<Diagnostic>? reportDiagnostic)
    {
        reportDiagnostic?.Invoke(Diagnostic.Create(
            ContractDiagnostics.Invalid,
            attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                ?? symbol.Locations.FirstOrDefault(),
            message));
    }
}
