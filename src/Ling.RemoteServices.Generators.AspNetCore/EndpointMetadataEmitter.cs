using Microsoft.CodeAnalysis;
using static Ling.RemoteServices.Generators.GeneratorUtilities;

namespace Ling.RemoteServices.Generators;

internal static class EndpointMetadataEmitter
{
    public static void EmitApply(
        CodeBuilder source,
        MethodModel method,
        HttpOperationModel operation,
        string operationVariable,
        bool supportsDotNet10,
        Action<Diagnostic>? reportDiagnostic)
    {
        var builderType = "global::Ling.RemoteServices.AspNetCore.RemoteServiceOperationConventionBuilder";
        foreach (var metadata in operation.EndpointMetadata)
        {
            switch (metadata.Kind)
            {
                case EndpointMetadataKind.Produces:
                    EmitProduces(source, metadata, operationVariable);
                    break;
                case EndpointMetadataKind.Accepts:
                    EmitAccepts(source, metadata, operationVariable);
                    break;
                case EndpointMetadataKind.Tags:
                    if (metadata.Values.Count > 0)
                    {
                        source.Append("global::Microsoft.AspNetCore.Http.OpenApiRouteHandlerBuilderExtensions.WithTags<")
                            .Append(builderType).Append(">(").Append(operationVariable).Append(", ")
                            .Append(StringArray(metadata.Values)).AppendLine(");");
                    }

                    break;
                case EndpointMetadataKind.Summary when metadata.Text is not null:
                    source.Append("global::Microsoft.AspNetCore.Http.OpenApiRouteHandlerBuilderExtensions.WithSummary<")
                        .Append(builderType).Append(">(").Append(operationVariable).Append(", \"")
                        .Append(Escape(metadata.Text)).AppendLine("\");");
                    break;
                case EndpointMetadataKind.Description when metadata.Text is not null:
                    source.Append("global::Microsoft.AspNetCore.Http.OpenApiRouteHandlerBuilderExtensions.WithDescription<")
                        .Append(builderType).Append(">(").Append(operationVariable).Append(", \"")
                        .Append(Escape(metadata.Text)).AppendLine("\");");
                    break;
                case EndpointMetadataKind.ExcludeFromDescription:
                    if (metadata.Boolean == true)
                    {
                        source.Append("global::Microsoft.AspNetCore.Http.OpenApiRouteHandlerBuilderExtensions.ExcludeFromDescription<")
                            .Append(builderType).Append(">(").Append(operationVariable).AppendLine(");");
                    }

                    break;
                case EndpointMetadataKind.Host when metadata.Values.Count > 0:
                    source.Append("global::Microsoft.AspNetCore.Builder.RoutingEndpointConventionBuilderExtensions.RequireHost<")
                        .Append(builderType).Append(">(").Append(operationVariable).Append(", ")
                        .Append(StringArray(metadata.Values)).AppendLine(");");
                    break;
                case EndpointMetadataKind.Order when metadata.Number is { } order:
                    source.Append("global::Microsoft.AspNetCore.Builder.RoutingEndpointConventionBuilderExtensions.WithOrder<")
                        .Append(builderType).Append(">(").Append(operationVariable).Append(", ")
                        .Append(order).AppendLine(");");
                    break;
                case EndpointMetadataKind.DisplayName when metadata.Text is not null:
                    source.Append("global::Microsoft.AspNetCore.Builder.RoutingEndpointConventionBuilderExtensions.WithDisplayName<")
                        .Append(builderType).Append(">(").Append(operationVariable).Append(", \"")
                        .Append(Escape(metadata.Text)).AppendLine("\");");
                    break;
                case EndpointMetadataKind.ShortCircuit:
                    source.Append("global::Microsoft.AspNetCore.Builder.RouteShortCircuitEndpointConventionBuilderExtensions.ShortCircuit(")
                        .Append(operationVariable);
                    if (metadata.Number is > 0)
                    {
                        source.Append(", ").Append(metadata.Number.Value);
                    }

                    source.AppendLine(");");
                    break;
                case EndpointMetadataKind.DisableAntiforgery:
                    if (metadata.Boolean == true)
                    {
                        source.Append("global::Microsoft.AspNetCore.Builder.RoutingEndpointConventionBuilderExtensions.DisableAntiforgery<")
                            .Append(builderType).Append(">(").Append(operationVariable).AppendLine(");");
                    }

                    break;
                case EndpointMetadataKind.DisableValidation:
                    if (metadata.Boolean == true)
                    {
                        if (supportsDotNet10)
                        {
                            source.Append("global::Microsoft.AspNetCore.Builder.ValidationEndpointConventionBuilderExtensions.DisableValidation<")
                                .Append(builderType).Append(">(").Append(operationVariable).AppendLine(");");
                        }
                        else
                        {
                            ReportUnsupported(reportDiagnostic, method, "RemoteDisableValidation requires an ASP.NET Core 10 host.");
                        }
                    }

                    break;
                case EndpointMetadataKind.AllowCookieRedirect:
                    if (metadata.Boolean == true)
                    {
                        if (supportsDotNet10)
                        {
                            source.Append("global::Microsoft.AspNetCore.Builder.CookieRedirectEndpointConventionBuilderExtensions.AllowCookieRedirect<")
                                .Append(builderType).Append(">(").Append(operationVariable).AppendLine(");");
                        }
                        else
                        {
                            ReportUnsupported(reportDiagnostic, method, "RemoteAllowCookieRedirect requires an ASP.NET Core 10 host.");
                        }
                    }

                    break;
                case EndpointMetadataKind.FormOptions:
                    EmitFormOptions(source, metadata, builderType, operationVariable);
                    break;
                case EndpointMetadataKind.FormMappingOptions:
                    EmitFormMappingOptions(source, metadata, builderType, operationVariable);
                    break;
            }
        }
    }

    public static bool HasExplicitAccepts(HttpOperationModel operation) =>
        operation.EndpointMetadata.Any(item => item.Kind == EndpointMetadataKind.Accepts);

    public static bool HasExplicitProducesStatus(HttpOperationModel operation, int statusCode) =>
        operation.EndpointMetadata.Any(item =>
            item.Kind == EndpointMetadataKind.Produces && item.StatusCode == statusCode);

    private static void EmitProduces(CodeBuilder source, EndpointMetadataModel metadata, string operationVariable)
    {
        var type = metadata.Type is not null
            ? TypeName(metadata.Type)
            : metadata.NamedValues.TryGetValue("IsValidationProblem", out var validation) && validation is true
                ? "global::Microsoft.AspNetCore.Http.HttpValidationProblemDetails"
                : metadata.NamedValues.TryGetValue("IsProblem", out var isProblem) && isProblem is true
                    ? "global::Microsoft.AspNetCore.Mvc.ProblemDetails"
                    : "global::System.Void";
        var types = new List<string>();
        if (!string.IsNullOrWhiteSpace(metadata.ContentType))
        {
            types.Add(metadata.ContentType!);
        }
        else if (type != "global::System.Void")
        {
            types.Add(metadata.NamedValues.TryGetValue("IsProblem", out var problemContentType) && problemContentType is true
                ? "application/problem+json"
                : "application/json");
        }

        types.AddRange(metadata.ContentTypes);
        source.Append(operationVariable).Append(".WithMetadata(new global::Microsoft.AspNetCore.Http.ProducesResponseTypeMetadata(")
            .Append(metadata.StatusCode ?? 200).Append(", typeof(").Append(type).Append("), ")
            .Append(StringArray(types)).AppendLine("));");
    }

    private static void EmitAccepts(CodeBuilder source, EndpointMetadataModel metadata, string operationVariable)
    {
        var types = new List<string>();
        if (!string.IsNullOrWhiteSpace(metadata.ContentType))
        {
            types.Add(metadata.ContentType!);
        }

        types.AddRange(metadata.ContentTypes);
        source.Append(operationVariable).Append(".WithMetadata(new global::Microsoft.AspNetCore.Http.Metadata.AcceptsMetadata(")
            .Append(StringArray(types)).Append(", typeof(")
            .Append(metadata.Type is null ? "global::System.Object" : TypeName(metadata.Type))
            .Append("), ").Append(metadata.Boolean == true ? "true" : "false").AppendLine("));");
    }

    private static void EmitFormOptions(
        CodeBuilder source,
        EndpointMetadataModel metadata,
        string builderType,
        string operationVariable)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["BufferBody"] = "bufferBody",
            ["MemoryBufferThreshold"] = "memoryBufferThreshold",
            ["BufferBodyLengthLimit"] = "bufferBodyLengthLimit",
            ["ValueCountLimit"] = "valueCountLimit",
            ["KeyLengthLimit"] = "keyLengthLimit",
            ["ValueLengthLimit"] = "valueLengthLimit",
            ["MultipartBoundaryLengthLimit"] = "multipartBoundaryLengthLimit",
            ["MultipartHeadersCountLimit"] = "multipartHeadersCountLimit",
            ["MultipartHeadersLengthLimit"] = "multipartHeadersLengthLimit",
            ["MultipartBodyLengthLimit"] = "multipartBodyLengthLimit"
        };
        var arguments = new List<string>();
        foreach (var parameter in parameters)
        {
            if (metadata.NamedValues.TryGetValue(parameter.Key, out var value)
                && value is not null
                && !(value is int intValue && intValue < 0)
                && !(value is long longValue && longValue < 0))
            {
                var literal = value is bool boolValue ? (boolValue ? "true" : "false") : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
                arguments.Add(parameter.Value + ": " + literal);
            }
        }

        if (arguments.Count == 0)
        {
            return;
        }

        source.Append("global::Microsoft.AspNetCore.Builder.RoutingEndpointConventionBuilderExtensions.WithFormOptions<")
            .Append(builderType).Append(">(").Append(operationVariable).Append(", ")
            .Append(string.Join(", ", arguments)).AppendLine(");");
    }

    private static void EmitFormMappingOptions(
        CodeBuilder source,
        EndpointMetadataModel metadata,
        string builderType,
        string operationVariable)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["MaxCollectionSize"] = "maxCollectionSize",
            ["MaxRecursionDepth"] = "maxRecursionDepth",
            ["MaxKeySize"] = "maxKeySize"
        };
        var arguments = new List<string>();
        foreach (var parameter in parameters)
        {
            if (metadata.NamedValues.TryGetValue(parameter.Key, out var value)
                && value is int intValue
                && intValue >= 0)
            {
                arguments.Add(parameter.Value + ": " + intValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        if (arguments.Count == 0)
        {
            return;
        }

        source.Append("global::Microsoft.AspNetCore.Builder.RoutingEndpointConventionBuilderExtensions.WithFormMappingOptions<")
            .Append(builderType).Append(">(").Append(operationVariable).Append(", ")
            .Append(string.Join(", ", arguments)).AppendLine(");");
    }

    private static string StringArray(IEnumerable<string> values) =>
        "new string[] { " + string.Join(", ", values.Select(value => "\"" + Escape(value) + "\"")) + " }";

    private static void ReportUnsupported(Action<Diagnostic>? reportDiagnostic, MethodModel method, string message)
    {
        reportDiagnostic?.Invoke(Diagnostic.Create(
            ContractDiagnostics.UnsupportedFrameworkFeature,
            method.Symbol.Locations.FirstOrDefault(),
            message));
    }
}
