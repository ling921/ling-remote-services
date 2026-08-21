using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using static Ling.RemoteServices.Generators.GeneratorUtilities;

namespace Ling.RemoteServices.Generators;

internal static class ServerEmitter
{
    public static SourceText EmitService(ServiceModel service, string rootNamespace)
    {
        var source = CreateSource(rootNamespace);
        var mapperName = GetGeneratedTypeName(service.Symbol, "EndpointMapper");

        source.AppendGeneratedTypeAttributes()
            .Append("internal static class ")
            .AppendLine(mapperName)
            .OpenBrace()
            .Append("internal static global::Ling.RemoteServices.AspNetCore.RemoteServiceEndpointConventionBuilder<")
            .Append(TypeName(service.Symbol))
            .AppendLine("> Map(")
            .IncreaseIndentLevel()
            .AppendLine("global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)")
            .DecreaseIndentLevel()
            .OpenBrace()
            .Append("var group = global::Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions.MapGroup(endpoints, \"")
            .Append(Escape(service.RoutePrefix))
            .AppendLine("\");")
            .Append("group.WithTags(\"")
            .Append(Escape(service.Symbol.Name))
            .AppendLine("\");")
            .AppendLine("var operations = new global::System.Collections.Generic.Dictionary<string, global::Ling.RemoteServices.AspNetCore.RemoteServiceMethodConventionBuilder>(global::System.StringComparer.Ordinal);");

        var operationIndex = 0;
        for (var methodIndex = 0; methodIndex < service.Methods.Count; methodIndex++)
        {
            var method = service.Methods[methodIndex];
            source
                .AppendLine()
                .Append("var methodOperations")
                .Append(methodIndex)
                .AppendLine(" = new global::System.Collections.Generic.Dictionary<global::Ling.RemoteServices.RemoteHttpMethod, global::Microsoft.AspNetCore.Builder.IEndpointConventionBuilder>();");

            foreach (var operation in method.Operations)
            {
                EmitMap(source, service, method, operation, operationIndex, methodIndex);
                operationIndex++;
            }

            source
                .Append("operations.Add(\"")
                .Append(Escape(method.Symbol.Name))
                .Append("\", new global::Ling.RemoteServices.AspNetCore.RemoteServiceMethodConventionBuilder(methodOperations")
                .Append(methodIndex)
                .AppendLine("));");
        }

        source
            .AppendLine()
            .Append("return new global::Ling.RemoteServices.AspNetCore.RemoteServiceEndpointConventionBuilder<")
            .Append(TypeName(service.Symbol))
            .AppendLine(">(group, operations);")
            .CloseBrace();

        operationIndex = 0;
        foreach (var method in service.Methods)
        {
            foreach (var operation in method.Operations)
            {
                EmitDynamicMapMethod(
                    source,
                    service,
                    method,
                    operation,
                    operationIndex);
                operationIndex++;
            }
        }

        source.CloseBrace();

        return source.ToSourceText();
    }

    public static SourceText EmitRegistration(
        IReadOnlyList<ServiceModel> services,
        string rootNamespace)
    {
        var source = CreateSource(rootNamespace);
        source.AppendGeneratedTypeAttributes()
            .AppendLine("public static class RemoteServiceEndpointExtensions")
            .OpenBrace()
            .AppendLine("public static global::Ling.RemoteServices.AspNetCore.RemoteServiceEndpointConventionRegistry MapRemoteServices(")
            .IncreaseIndentLevel()
            .AppendLine("this global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints,")
            .AppendLine("global::System.Action<global::Ling.RemoteServices.AspNetCore.RemoteServiceEndpointConventionRegistry>? configure = null)")
            .DecreaseIndentLevel()
            .OpenBrace()
            .AppendLine("var services = new global::Ling.RemoteServices.AspNetCore.RemoteServiceEndpointConventionRegistry();");

        foreach (var service in services)
        {
            source
                .Append("services.AddService<")
                .Append(TypeName(service.Symbol))
                .Append(">(")
                .Append(GetGeneratedTypeName(service.Symbol, "EndpointMapper"))
                .AppendLine(".Map(endpoints));");
        }

        source
            .AppendLine()
            .AppendLine("configure?.Invoke(services);")
            .AppendLine("return services;")
            .CloseBrace()
            .CloseBrace();

        return source.ToSourceText();
    }

    private static CodeBuilder CreateSource(string rootNamespace)
    {
        return CreateGeneratedCodeBuilder()
            .AppendLine("using Microsoft.AspNetCore.Builder;")
            .AppendLine("using Microsoft.AspNetCore.Http;")
            .AppendLine()
            .Append("namespace ")
            .Append(rootNamespace)
            .AppendLine(".Generated;")
            .AppendLine();
    }

    private static void EmitMap(
        CodeBuilder source,
        ServiceModel service,
        MethodModel method,
        HttpOperationModel operation,
        int operationIndex,
        int methodIndex)
    {
        var httpContextParameterName = GetUniqueInfrastructureParameterName(
            operation,
            "__remoteServiceHttpContext");
        var serviceVariableName = GetUniqueInfrastructureParameterName(
            operation,
            "__remoteServiceImplementation");
        var jsonOptionsParameterName = UsesJson(method, operation)
            ? GetUniqueInfrastructureParameterName(operation, "__remoteServiceJsonOptions")
            : null;
        var formVariableName = operation.Parameters.Any(parameter => parameter.Kind == BindKind.Form)
            ? GetUniqueInfrastructureParameterName(operation, "__remoteServiceForm")
            : null;

        source
            .AppendLine()
            .Append("global::Microsoft.AspNetCore.Builder.IEndpointConventionBuilder operation")
            .Append(operationIndex)
            .AppendLine(";")
            .AppendLine("if (global::System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported)")
            .OpenBrace();

        EmitDynamicMapAssignment(
            source,
            operationIndex);

        source
            .CloseBrace()
            .AppendLine("else")
            .OpenBrace()
            .Append("operation")
            .Append(operationIndex)
            .AppendLine(" = global::Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions.MapMethods(")
            .IncreaseIndentLevel()
            .AppendLine("group,")
            .Append("\"")
            .Append(Escape(operation.RelativeRoute))
            .AppendLine("\",")
            .Append("new[] { \"")
            .Append(operation.Verb)
            .AppendLine("\" },")
            .Append("(global::Microsoft.AspNetCore.Http.RequestDelegate)(async ")
            .Append(httpContextParameterName)
            .AppendLine(" =>")
            .OpenBrace()
            .AppendLine("try")
            .OpenBrace()
            .Append("var ")
            .Append(serviceVariableName)
            .Append(" = global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<")
            .Append(TypeName(service.Symbol))
            .Append(">(")
            .Append(httpContextParameterName)
            .AppendLine(".RequestServices);");

        if (jsonOptionsParameterName is not null)
        {
            source
                .Append("var ")
                .Append(jsonOptionsParameterName)
                .Append(" = global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<")
                .Append("global::Microsoft.Extensions.Options.IOptions<global::Microsoft.AspNetCore.Http.Json.JsonOptions>>(")
                .Append(httpContextParameterName)
                .AppendLine(".RequestServices);");
        }

        if (formVariableName is not null)
        {
            source
                .Append("var ")
                .Append(formVariableName)
                .Append(" = await ")
                .Append(httpContextParameterName)
                .Append(".Request.ReadFormAsync(")
                .Append(httpContextParameterName)
                .AppendLine(".RequestAborted).ConfigureAwait(false);");
        }

        EmitParameterBindings(
            source,
            operation,
            httpContextParameterName,
            jsonOptionsParameterName,
            formVariableName);

        EmitResult(
            source,
            method,
            operation,
            BuildServiceCall(method, operation, serviceVariableName, useAspNetCoreBindings: false),
            jsonOptionsParameterName,
            httpContextParameterName);

        source
            .CloseBrace()
            .AppendLine("catch (global::Ling.RemoteServices.Exceptions.RemoteServiceException exception)")
            .OpenBrace()
            .Append("await global::Ling.RemoteServices.AspNetCore.RemoteServiceServerRuntime.Problem(exception).ExecuteAsync(")
            .Append(httpContextParameterName)
            .AppendLine(").ConfigureAwait(false);")
            .CloseBrace()
            .CloseBraceInline("));")
            .DecreaseIndentLevel()
            .CloseBrace()
            .Append("operation")
            .Append(operationIndex)
            .AppendLine()
            .IncreaseIndentLevel()
            .Append(".WithName(\"")
            .Append(Escape(GetOperationId(service, method, operation)))
            .Append("\")");

        if (!string.IsNullOrWhiteSpace(method.Summary))
        {
            source
                .Append(".WithSummary(\"")
                .Append(Escape(method.Summary!))
                .Append("\")");
        }

        EmitProducesMetadata(source, method, operation);
        EmitAcceptsMetadata(source, operation);
        source
            .AppendLine(";")
            .DecreaseIndentLevel();

        var operationVariable = "operation" + operationIndex;
        EndpointPolicyEmitter.EmitApply(source, service, method, operationVariable);

        source
            .Append("methodOperations")
            .Append(methodIndex)
            .Append(".Add(global::Ling.RemoteServices.RemoteHttpMethod.")
            .Append(GetRemoteHttpMethodName(operation.Verb))
            .Append(", operation")
            .Append(operationIndex)
            .AppendLine(");");
    }

    private static void EmitDynamicMapAssignment(
        CodeBuilder source,
        int operationIndex)
    {
        source
            .Append("operation")
            .Append(operationIndex)
            .Append(" = MapDynamicOperation")
            .Append(operationIndex)
            .AppendLine("(group);");
    }

    private static void EmitDynamicMapMethod(
        CodeBuilder source,
        ServiceModel service,
        MethodModel method,
        HttpOperationModel operation,
        int operationIndex)
    {
        var serviceVariableName = GetUniqueInfrastructureParameterName(
            operation,
            "__remoteServiceImplementation");
        var jsonOptionsParameterName = UsesJson(method, operation)
            ? GetUniqueInfrastructureParameterName(operation, "__remoteServiceJsonOptions")
            : null;
        var httpContextParameterName = GetUniqueInfrastructureParameterName(
            operation,
            "__remoteServiceHttpContext");

        source
            .AppendLine()
            .AppendLine("[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessageAttribute(\"Trimming\", \"IL2026\", Justification = \"Every handler parameter type is statically referenced by generated code.\")]")
            .AppendLine("[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessageAttribute(\"AOT\", \"IL3050\", Justification = \"This fallback is called only when dynamic code is supported.\")]")
            .Append("private static global::Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapDynamicOperation")
            .Append(operationIndex)
            .AppendLine("(global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder group)")
            .OpenBrace()
            .AppendLine("return global::Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions.MapMethods(")
            .IncreaseIndentLevel()
            .AppendLine("group,")
            .Append("\"")
            .Append(Escape(operation.RelativeRoute))
            .AppendLine("\",")
            .Append("new[] { \"")
            .Append(operation.Verb)
            .AppendLine("\" },")
            .AppendLine("async (")
            .IncreaseIndentLevel();

        var handlerParameters = GetHandlerParameters(
            service,
            operation,
            serviceVariableName,
            jsonOptionsParameterName,
            httpContextParameterName);
        for (var index = 0; index < handlerParameters.Count; index++)
        {
            source
                .Append(handlerParameters[index])
                .AppendLine(index == handlerParameters.Count - 1 ? string.Empty : ",");
        }

        source
            .DecreaseIndentLevel()
            .AppendLine(") =>")
            .OpenBrace()
            .AppendLine("try")
            .OpenBrace();

        EmitResult(
            source,
            method,
            operation,
            BuildServiceCall(method, operation, serviceVariableName, useAspNetCoreBindings: true),
            jsonOptionsParameterName,
            httpContextParameterName);

        source
            .CloseBrace()
            .AppendLine("catch (global::Ling.RemoteServices.Exceptions.RemoteServiceException exception)")
            .OpenBrace()
            .Append("await global::Ling.RemoteServices.AspNetCore.RemoteServiceServerRuntime.Problem(exception).ExecuteAsync(")
            .Append(httpContextParameterName)
            .AppendLine(").ConfigureAwait(false);")
            .CloseBrace()
            .CloseBraceInline(");")
            .DecreaseIndentLevel()
            .CloseBrace();
    }

    private static List<string> GetHandlerParameters(
        ServiceModel service,
        HttpOperationModel operation,
        string serviceVariableName,
        string? jsonOptionsParameterName,
        string httpContextParameterName)
    {
        var parameters = new List<string>
        {
            "[global::Microsoft.AspNetCore.Mvc.FromServices] "
                + TypeName(service.Symbol)
                + " "
                + serviceVariableName
        };

        if (jsonOptionsParameterName is not null)
        {
            parameters.Add(
                "[global::Microsoft.AspNetCore.Mvc.FromServices] "
                + "global::Microsoft.Extensions.Options.IOptions<global::Microsoft.AspNetCore.Http.Json.JsonOptions> "
                + jsonOptionsParameterName);
        }

        parameters.Add(
            "global::Microsoft.AspNetCore.Http.HttpContext "
            + httpContextParameterName);

        foreach (var parameter in operation.Parameters)
        {
            if (IsComplexQuery(parameter))
            {
                foreach (var property in GetQueryProperties(parameter))
                {
                    parameters.Add(
                        $"[global::Microsoft.AspNetCore.Mvc.FromQuery(Name=\"{Escape(property.Name)}\")] "
                        + $"{TypeName(property.Type)} {parameter.Symbol.Name}_{property.Name}");
                }

                continue;
            }

            var isUpload = IsUploadFile(parameter);
            var parameterType = isUpload
                ? "global::Microsoft.AspNetCore.Http.IFormFile"
                : TypeName(parameter.Symbol.Type);
            var bindingAttribute = GetBindingAttribute(parameter);
            parameters.Add(bindingAttribute + parameterType + " " + parameter.Symbol.Name);
        }

        return parameters;
    }

    private static string GetBindingAttribute(ParameterModel parameter)
    {
        return parameter.Kind switch
        {
            BindKind.Path => $"[global::Microsoft.AspNetCore.Mvc.FromRoute(Name=\"{Escape(parameter.Name)}\")] ",
            BindKind.Query => $"[global::Microsoft.AspNetCore.Mvc.FromQuery(Name=\"{Escape(parameter.Name)}\")] ",
            BindKind.Header => $"[global::Microsoft.AspNetCore.Mvc.FromHeader(Name=\"{Escape(parameter.Name)}\")] ",
            BindKind.Body => "[global::Microsoft.AspNetCore.Mvc.FromBody] ",
            BindKind.Form => $"[global::Microsoft.AspNetCore.Mvc.FromForm(Name=\"{Escape(parameter.Name)}\")] ",
            _ => string.Empty
        };
    }

    private static string BuildServiceCall(
        MethodModel method,
        HttpOperationModel operation,
        string serviceVariableName,
        bool useAspNetCoreBindings)
    {
        var arguments = operation.Parameters.Select(parameter =>
        {
            if (IsComplexQuery(parameter))
            {
                var assignments = GetQueryProperties(parameter)
                    .Select(property =>
                        property.Name + " = " + parameter.Symbol.Name + "_" + property.Name);
                return $"new {TypeName(parameter.Symbol.Type)} {{ {string.Join(", ", assignments)} }}";
            }

            if (useAspNetCoreBindings && IsUploadFile(parameter))
            {
                return "new global::Ling.RemoteServices.Models.RemoteUploadFile("
                    + $"{parameter.Symbol.Name}.OpenReadStream(), "
                    + $"{parameter.Symbol.Name}.FileName, "
                    + $"{parameter.Symbol.Name}.ContentType, "
                    + $"{parameter.Symbol.Name}.Length, false)";
            }

            return parameter.Symbol.Name;
        });

        return serviceVariableName + "." + method.Symbol.Name + "(" + string.Join(", ", arguments) + ")";
    }

    private static void EmitParameterBindings(
        CodeBuilder source,
        HttpOperationModel operation,
        string httpContextVariableName,
        string? jsonOptionsVariableName,
        string? formVariableName)
    {
        foreach (var parameter in operation.Parameters)
        {
            if (parameter.Kind == BindKind.Cancellation)
            {
                source
                    .Append("var ")
                    .Append(parameter.Symbol.Name)
                    .Append(" = ")
                    .Append(httpContextVariableName)
                    .AppendLine(".RequestAborted;");
                continue;
            }

            if (parameter.Kind == BindKind.Body)
            {
                source
                    .Append("var ")
                    .Append(parameter.Symbol.Name)
                    .Append(" = (await ")
                    .Append(httpContextVariableName)
                    .Append(".Request.ReadFromJsonAsync(")
                    .Append("global::Ling.RemoteServices.AspNetCore.RemoteServiceServerRuntime.GetJsonTypeInfo<")
                    .Append(TypeName(parameter.Symbol.Type))
                    .Append(">(")
                    .Append(jsonOptionsVariableName!)
                    .Append(".Value.SerializerOptions), ")
                    .Append(httpContextVariableName)
                    .AppendLine(".RequestAborted).ConfigureAwait(false))");

                if (!IsNullable(parameter.Symbol.Type, parameter.Symbol.NullableAnnotation))
                {
                    source
                        .IncreaseIndentLevel()
                        .Append("?? throw new global::Microsoft.AspNetCore.Http.BadHttpRequestException(\"Required JSON body '")
                        .Append(Escape(parameter.Name))
                        .AppendLine("' was not provided.\", 400);")
                        .DecreaseIndentLevel();
                }
                else
                {
                    source.AppendLine(";");
                }

                continue;
            }

            if (IsComplexQuery(parameter))
            {
                EmitComplexQueryBinding(source, parameter, httpContextVariableName);
                continue;
            }

            if (IsUploadFile(parameter))
            {
                source
                    .Append("var ")
                    .Append(parameter.Symbol.Name)
                    .Append("File = ")
                    .Append(formVariableName!)
                    .Append(".Files.GetFile(\"")
                    .Append(Escape(parameter.Name))
                    .AppendLine("\")")
                    .IncreaseIndentLevel()
                    .Append("?? throw new global::Microsoft.AspNetCore.Http.BadHttpRequestException(\"Required file '")
                    .Append(Escape(parameter.Name))
                    .AppendLine("' was not provided.\", 400);")
                    .DecreaseIndentLevel()
                    .Append("var ")
                    .Append(parameter.Symbol.Name)
                    .Append(" = new global::Ling.RemoteServices.Models.RemoteUploadFile(")
                    .Append(parameter.Symbol.Name)
                    .Append("File.OpenReadStream(), ")
                    .Append(parameter.Symbol.Name)
                    .Append("File.FileName, ")
                    .Append(parameter.Symbol.Name)
                    .Append("File.ContentType, ")
                    .Append(parameter.Symbol.Name)
                    .AppendLine("File.Length, false);");
                continue;
            }

            if (parameter.Symbol.Type is IArrayTypeSymbol arrayType
                && parameter.Kind == BindKind.Query)
            {
                source
                    .Append("var ")
                    .Append(parameter.Symbol.Name)
                    .Append(" = global::Ling.RemoteServices.AspNetCore.RemoteServiceServerRuntime.GetQueryValues(")
                    .Append(httpContextVariableName)
                    .Append(", \"")
                    .Append(Escape(parameter.Name))
                    .Append("\").Select(static value => ")
                    .Append(GetScalarParseExpression(arrayType.ElementType, "value"))
                    .AppendLine(").ToArray();");
                continue;
            }

            var valueExpression = parameter.Kind switch
            {
                BindKind.Path => "global::Ling.RemoteServices.AspNetCore.RemoteServiceServerRuntime.GetRouteValue("
                    + httpContextVariableName + ", \"" + Escape(parameter.Name) + "\")",
                BindKind.Query => "global::Ling.RemoteServices.AspNetCore.RemoteServiceServerRuntime.GetQueryValue("
                    + httpContextVariableName + ", \"" + Escape(parameter.Name) + "\")",
                BindKind.Header => "global::Ling.RemoteServices.AspNetCore.RemoteServiceServerRuntime.GetHeaderValue("
                    + httpContextVariableName + ", \"" + Escape(parameter.Name) + "\")",
                BindKind.Form => "global::Ling.RemoteServices.AspNetCore.RemoteServiceServerRuntime.GetFormValue("
                    + formVariableName + ", \"" + Escape(parameter.Name) + "\")",
                _ => throw new InvalidOperationException("Unsupported generated binding kind.")
            };

            EmitScalarBinding(
                source,
                parameter.Symbol.Name,
                parameter.Name,
                parameter.Symbol.Type,
                parameter.Symbol.NullableAnnotation,
                valueExpression);
        }
    }

    private static void EmitComplexQueryBinding(
        CodeBuilder source,
        ParameterModel parameter,
        string httpContextVariableName)
    {
        foreach (var property in GetQueryProperties(parameter))
        {
            EmitScalarBinding(
                source,
                parameter.Symbol.Name + "_" + property.Name,
                property.Name,
                property.Type,
                property.NullableAnnotation,
                "global::Ling.RemoteServices.AspNetCore.RemoteServiceServerRuntime.GetQueryValue("
                    + httpContextVariableName + ", \"" + Escape(property.Name) + "\")");
        }
    }

    private static void EmitScalarBinding(
        CodeBuilder source,
        string variableName,
        string protocolName,
        ITypeSymbol type,
        NullableAnnotation nullableAnnotation,
        string valueExpression)
    {
        var nullable = IsNullable(type, nullableAnnotation);
        var underlyingType = UnwrapNullable(type);

        if (underlyingType.SpecialType == SpecialType.System_String)
        {
            source
                .Append("var ")
                .Append(variableName)
                .Append(" = ");

            if (!nullable)
            {
                source.Append("global::Ling.RemoteServices.AspNetCore.RemoteServiceServerRuntime.RequireValue(");
            }

            source.Append(valueExpression);
            if (!nullable)
            {
                source
                    .Append(", \"")
                    .Append(Escape(protocolName))
                    .Append("\")");
            }

            source.AppendLine(";");
            return;
        }

        var rawVariableName = "__remoteServiceRaw_" + variableName;
        source
            .Append("var ")
            .Append(rawVariableName)
            .Append(" = ")
            .Append(valueExpression)
            .AppendLine(";")
            .Append("var ")
            .Append(variableName)
            .Append(" = ");

        if (nullable)
        {
            source
                .Append(rawVariableName)
                .Append(" is null ? (")
                .Append(TypeName(type))
                .Append(")null : ")
                .Append(GetScalarParseExpression(underlyingType, rawVariableName));
        }
        else
        {
            var requiredExpression = "global::Ling.RemoteServices.AspNetCore.RemoteServiceServerRuntime.RequireValue("
                + rawVariableName + ", \"" + Escape(protocolName) + "\")";
            source.Append(GetScalarParseExpression(underlyingType, requiredExpression));
        }

        source.AppendLine(";");
    }

    private static string GetScalarParseExpression(ITypeSymbol type, string valueExpression)
    {
        var typeName = TypeName(type);
        if (type.TypeKind == TypeKind.Enum)
        {
            return "global::System.Enum.Parse<" + typeName + ">(" + valueExpression + ", true)";
        }

        if (type.SpecialType == SpecialType.System_String)
        {
            return valueExpression;
        }

        if (type.ToDisplayString() == "System.Uri")
        {
            return "new global::System.Uri(" + valueExpression + ", global::System.UriKind.RelativeOrAbsolute)";
        }

        if (type.SpecialType is SpecialType.System_Boolean or SpecialType.System_Char)
        {
            return typeName + ".Parse(" + valueExpression + ")";
        }

        if (type.ToDisplayString() == "System.Guid")
        {
            return "global::System.Guid.Parse(" + valueExpression + ")";
        }

        return typeName + ".Parse("
            + valueExpression
            + ", global::System.Globalization.CultureInfo.InvariantCulture)";
    }

    private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        return type is INamedTypeSymbol namedType
            && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            ? namedType.TypeArguments[0]
            : type;
    }

    private static bool IsNullable(ITypeSymbol type, NullableAnnotation nullableAnnotation)
    {
        return nullableAnnotation == NullableAnnotation.Annotated
            || type is INamedTypeSymbol namedType
                && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
    }

    private static bool UsesJson(MethodModel method, HttpOperationModel operation)
    {
        return operation.Parameters.Any(parameter => parameter.Kind == BindKind.Body)
            || GetJsonOptionsParameterName(method, operation) is not null;
    }

    private static void EmitResult(
        CodeBuilder source,
        MethodModel method,
        HttpOperationModel operation,
        string serviceCall,
        string? jsonOptionsParameterName,
        string httpContextParameterName)
    {
        if (method.Result is null)
        {
            source
                .Append("await ")
                .Append(serviceCall)
                .AppendLine(".ConfigureAwait(false);")
                .Append("await global::Microsoft.AspNetCore.Http.Results.StatusCode(")
                .Append(operation.SuccessStatus ?? 204)
                .Append(").ExecuteAsync(")
                .Append(httpContextParameterName)
                .AppendLine(").ConfigureAwait(false);");
            return;
        }

        if (method.Result.ToDisplayString() == "Ling.RemoteServices.Models.RemoteFile")
        {
            source
                .Append("var file = await ")
                .Append(serviceCall)
                .AppendLine(".ConfigureAwait(false);")
                .AppendLine("await global::Microsoft.AspNetCore.Http.TypedResults.Stream(")
                .IncreaseIndentLevel()
                .AppendLine("file.Content,")
                .AppendLine("file.ContentType,")
                .AppendLine("file.FileName,")
                .AppendLine("file.LastModified,")
                .AppendLine("entityTag: null,")
                .Append("enableRangeProcessing: file.EnableRangeProcessing).ExecuteAsync(")
                .Append(httpContextParameterName)
                .AppendLine(").ConfigureAwait(false);")
                .DecreaseIndentLevel();
            return;
        }

        source
            .Append("var result = await ")
            .Append(serviceCall)
            .AppendLine(".ConfigureAwait(false);");

        var isText = operation.ResponseContentType?.StartsWith("text/", StringComparison.OrdinalIgnoreCase) == true
            && method.Result.SpecialType == SpecialType.System_String;
        if (isText)
        {
            source
                .Append("await global::Microsoft.AspNetCore.Http.Results.Text(result, \"")
                .Append(Escape(operation.ResponseContentType!))
                .Append("\", statusCode: ")
                .Append(operation.SuccessStatus ?? 200)
                .Append(").ExecuteAsync(")
                .Append(httpContextParameterName)
                .AppendLine(").ConfigureAwait(false);");
            return;
        }

        var isByteArray = operation.ResponseContentType is not null
            && method.Result is IArrayTypeSymbol arrayType
            && arrayType.ElementType.SpecialType == SpecialType.System_Byte;
        if (isByteArray)
        {
            source
                .Append("await global::Microsoft.AspNetCore.Http.Results.Bytes(result, \"")
                .Append(Escape(operation.ResponseContentType!))
                .Append("\").ExecuteAsync(")
                .Append(httpContextParameterName)
                .AppendLine(").ConfigureAwait(false);");
            return;
        }

        source
            .Append("await global::Microsoft.AspNetCore.Http.Results.Json(result, ")
            .Append("global::Ling.RemoteServices.AspNetCore.RemoteServiceServerRuntime.GetJsonTypeInfo<")
            .Append(TypeName(method.Result))
            .Append(">(")
            .Append(jsonOptionsParameterName!)
            .Append(".Value.SerializerOptions), statusCode: ")
            .Append(operation.SuccessStatus ?? 200)
            .Append(").ExecuteAsync(")
            .Append(httpContextParameterName)
            .AppendLine(").ConfigureAwait(false);");
    }

    private static string? GetJsonOptionsParameterName(
        MethodModel method,
        HttpOperationModel operation)
    {
        if (method.Result is null
            || method.Result.ToDisplayString() == "Ling.RemoteServices.Models.RemoteFile"
            || operation.ResponseContentType?.StartsWith("text/", StringComparison.OrdinalIgnoreCase) == true
            || operation.ResponseContentType is not null
                && method.Result is IArrayTypeSymbol arrayType
                && arrayType.ElementType.SpecialType == SpecialType.System_Byte)
        {
            return null;
        }

        return GetUniqueInfrastructureParameterName(
            operation,
            "__remoteServiceJsonOptions");
    }

    private static string GetUniqueInfrastructureParameterName(
        HttpOperationModel operation,
        string preferredName)
    {
        var parameterNames = new HashSet<string>(
            operation.Parameters.Select(parameter => parameter.Symbol.Name),
            StringComparer.Ordinal);
        var name = preferredName;

        while (parameterNames.Contains(name))
        {
            name += "_";
        }

        return name;
    }

    private static void EmitProducesMetadata(
        CodeBuilder source,
        MethodModel method,
        HttpOperationModel operation)
    {
        if (method.Result is null)
        {
            source
                .Append(".WithMetadata(new global::Microsoft.AspNetCore.Http.ProducesResponseTypeMetadata(")
                .Append(operation.SuccessStatus ?? 204)
                .Append(", typeof(void), global::System.Array.Empty<string>()))");
            return;
        }

        source
            .Append(".WithMetadata(new global::Microsoft.AspNetCore.Http.ProducesResponseTypeMetadata(")
            .Append(operation.SuccessStatus ?? 200)
            .Append(", typeof(")
            .Append(RuntimeTypeName(method.Result))
            .Append("), new[] { \"")
            .Append(Escape(operation.ResponseContentType ?? "application/json"))
            .Append("\" }))");
    }

    private static void EmitAcceptsMetadata(
        CodeBuilder source,
        HttpOperationModel operation)
    {
        var body = operation.Parameters.FirstOrDefault(parameter => parameter.Kind == BindKind.Body);
        if (body is not null)
        {
            source
                .Append(".WithMetadata(new global::Microsoft.AspNetCore.Http.Metadata.AcceptsMetadata(")
                .Append("new[] { \"application/json\" }, typeof(")
                .Append(RuntimeTypeName(body.Symbol.Type))
                .Append("), ")
                .Append(IsNullable(body.Symbol.Type, body.Symbol.NullableAnnotation) ? "true" : "false")
                .Append("))");
        }

        if (operation.Parameters.Any(parameter => parameter.Kind == BindKind.Form))
        {
            source
                .Append(".WithMetadata(new global::Microsoft.AspNetCore.Http.Metadata.AcceptsMetadata(")
                .Append("new[] { \"multipart/form-data\" }, typeof(global::Microsoft.AspNetCore.Http.IFormCollection), false))")
                .Append(".WithMetadata(global::Ling.RemoteServices.AspNetCore.RemoteServiceAntiforgeryMetadata.Required)");
        }
    }

    private static string GetOperationId(
        ServiceModel service,
        MethodModel method,
        HttpOperationModel operation)
    {
        var operationId = service.Symbol.Name + "_" + method.Symbol.Name;
        return method.Operations.Count == 1
            ? operationId
            : operationId + "_" + operation.Verb;
    }

    private static bool IsUploadFile(ParameterModel parameter)
    {
        return parameter.Kind == BindKind.Form
            && parameter.Symbol.Type.ToDisplayString()
                == "Ling.RemoteServices.Models.RemoteUploadFile";
    }
}
