using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using static Ling.RemoteServices.Generators.GeneratorUtilities;

namespace Ling.RemoteServices.Generators;

internal static class ClientEmitter
{
    public static SourceText EmitService(ServiceModel service, string rootNamespace)
    {
        var source = CreateSource(rootNamespace);
        EmitClient(source, service);
        return source.ToSourceText();
    }

    public static SourceText EmitRegistration(
        IReadOnlyList<ServiceModel> services,
        string rootNamespace)
    {
        var source = CreateSource(rootNamespace);
        EmitRegistrationExtensions(source, services);
        return source.ToSourceText();
    }

    private static CodeBuilder CreateSource(string rootNamespace)
    {
        return CreateGeneratedCodeBuilder()
            .AppendLine("using Microsoft.Extensions.DependencyInjection;")
            .AppendLine()
            .Append("namespace ")
            .Append(rootNamespace)
            .AppendLine(".Generated;")
            .AppendLine();
    }

    private static void EmitRegistrationExtensions(
        CodeBuilder source,
        IReadOnlyList<ServiceModel> services)
    {
        source.AppendGeneratedTypeAttributes()
            .AppendLine("public static class RemoteServiceClientRegistrationExtensions")
            .OpenBrace()
            .AppendLine("public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddRemoteServiceClients(")
            .IncreaseIndentLevel()
            .AppendLine("this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services,")
            .AppendLine("global::System.Action<global::Ling.RemoteServices.Client.RemoteServiceClientOptions>? configure = null)")
            .DecreaseIndentLevel()
            .OpenBrace()
            .AppendLine("var options = new global::Ling.RemoteServices.Client.RemoteServiceClientOptions();")
            .AppendLine("configure?.Invoke(options);")
            .AppendLine("services.AddSingleton(options);");

        foreach (var service in services)
        {
            source
                .Append("services.AddScoped<")
                .Append(TypeName(service.Symbol))
                .Append(", ")
                .Append(GetGeneratedTypeName(service.Symbol, "Client"))
                .AppendLine(">();");
        }

        source
            .AppendLine()
            .AppendLine("return services;")
            .CloseBrace()
            .CloseBrace();
    }

    private static void EmitClient(CodeBuilder source, ServiceModel service)
    {
        source.AppendGeneratedTypeAttributes()
            .Append("internal sealed class ")
            .Append(GetGeneratedTypeName(service.Symbol, "Client"))
            .AppendLine()
            .IncreaseIndentLevel()
            .Append(": ")
            .Append(TypeName(service.Symbol))
            .Append(", global::Ling.RemoteServices.Client.IRemoteHttpMethodSelectable<")
            .Append(TypeName(service.Symbol))
            .AppendLine(">")
            .DecreaseIndentLevel()
            .OpenBrace()
            .AppendLine("private readonly global::System.Net.Http.HttpClient http;")
            .AppendLine("private readonly global::Ling.RemoteServices.Client.RemoteServiceClientOptions options;")
            .AppendLine("private readonly global::Ling.RemoteServices.RemoteHttpMethod? selectedHttpMethod;")
            .AppendLine()
            .Append("public ")
            .Append(GetGeneratedTypeName(service.Symbol, "Client"))
            .AppendLine("(")
            .IncreaseIndentLevel()
            .AppendLine("global::System.Net.Http.HttpClient http,")
            .AppendLine("global::Ling.RemoteServices.Client.RemoteServiceClientOptions options)")
            .AppendLine(": this(http, options, null)")
            .DecreaseIndentLevel()
            .OpenBrace()
            .CloseBrace()
            .AppendLine()
            .Append("private ")
            .Append(GetGeneratedTypeName(service.Symbol, "Client"))
            .AppendLine("(")
            .IncreaseIndentLevel()
            .AppendLine("global::System.Net.Http.HttpClient http,")
            .AppendLine("global::Ling.RemoteServices.Client.RemoteServiceClientOptions options,")
            .AppendLine("global::Ling.RemoteServices.RemoteHttpMethod? selectedHttpMethod)")
            .DecreaseIndentLevel()
            .OpenBrace()
            .AppendLine("this.http = http;")
            .AppendLine("this.options = options;")
            .AppendLine("this.selectedHttpMethod = selectedHttpMethod;")
            .CloseBrace()
            .AppendLine()
            .Append(TypeName(service.Symbol))
            .Append(" global::Ling.RemoteServices.Client.IRemoteHttpMethodSelectable<")
            .Append(TypeName(service.Symbol))
            .AppendLine(">.WithHttpMethod(global::Ling.RemoteServices.RemoteHttpMethod method)")
            .OpenBrace()
            .Append("return new ")
            .Append(GetGeneratedTypeName(service.Symbol, "Client"))
            .AppendLine("(this.http, this.options, method);")
            .CloseBrace()
            .AppendLine();

        foreach (var method in service.Methods)
        {
            EmitMethod(source, method, service.Symbol);
        }

        source.CloseBrace().AppendLine();
    }

    private static void EmitMethod(
        CodeBuilder source,
        MethodModel method,
        INamedTypeSymbol service)
    {
        EmitMethodSignature(source, method);
        var names = new ClientMethodVariableNames(method.Symbol);

        source
            .OpenBrace()
            .Append("var ")
            .Append(names.HttpMethod)
            .Append(" = this.selectedHttpMethod ?? global::Ling.RemoteServices.RemoteHttpMethod.")
            .Append(GetRemoteHttpMethodName(method.ClientDefaultOperation.Verb))
            .AppendLine(";");

        EmitHttpMethodValidation(source, method, names.HttpMethod);

        source
            .AppendLine("try")
            .OpenBrace();

        var cancellationToken = method.Symbol.Parameters
            .FirstOrDefault(parameter => parameter.Type.ToDisplayString()
                == "System.Threading.CancellationToken")
            ?.Name
            ?? "global::System.Threading.CancellationToken.None";
        var returnsFile = method.Result?.ToDisplayString()
            == "Ling.RemoteServices.Models.RemoteFile";

        foreach (var operation in method.Operations)
        {
            source
                .Append("if (")
                .Append(names.HttpMethod)
                .Append(" == global::Ling.RemoteServices.RemoteHttpMethod.")
                .Append(GetRemoteHttpMethodName(operation.Verb))
                .AppendLine(")")
                .OpenBrace();

            EmitPathAndQuery(source, operation, names);
            EmitRequest(source, operation, names);
            EmitSend(source, method, operation, service, cancellationToken, returnsFile, names);
            EmitResponse(source, method, operation, cancellationToken, returnsFile, names);

            source.CloseBrace();
        }

        source.AppendLine("throw new global::System.InvalidOperationException(\"The selected HTTP method was not generated for this operation.\");");
        EmitExceptionHandling(source, method, service);
    }

    private static void EmitHttpMethodValidation(
        CodeBuilder source,
        MethodModel method,
        string httpMethodVariable)
    {
        source.Append("if (");
        for (var index = 0; index < method.Operations.Count; index++)
        {
            if (index > 0)
            {
                source.Append(" && ");
            }

            source
                .Append(httpMethodVariable)
                .Append(" != global::Ling.RemoteServices.RemoteHttpMethod.")
                .Append(GetRemoteHttpMethodName(method.Operations[index].Verb));
        }

        source
            .AppendLine(")")
            .OpenBrace()
            .Append("throw new global::System.InvalidOperationException(\"Remote method '")
            .Append(Escape(method.Symbol.Name))
            .Append("' does not expose the selected HTTP method. Available methods: ")
            .Append(string.Join(", ", method.Operations.Select(operation => operation.Verb)))
            .AppendLine(".\");")
            .CloseBrace()
            .AppendLine();
    }

    private static void EmitMethodSignature(CodeBuilder source, MethodModel method)
    {
        var parameters = method.Symbol.Parameters.Select(parameter =>
        {
            var defaultValue = parameter.HasExplicitDefaultValue ? " = default" : string.Empty;
            return TypeName(parameter.Type) + " " + parameter.Name + defaultValue;
        }).ToList();

        source
            .Append("public async ")
            .Append(TypeName(method.Symbol.ReturnType))
            .Append(' ')
            .Append(method.Symbol.Name);

        if (parameters.Count == 0)
        {
            source.AppendLine("()");
            return;
        }

        source
            .AppendLine("(")
            .IncreaseIndentLevel();

        for (var index = 0; index < parameters.Count; index++)
        {
            source
                .Append(parameters[index])
                .AppendLine(index == parameters.Count - 1 ? string.Empty : ",");
        }

        source
            .DecreaseIndentLevel()
            .AppendLine(")");
    }

    private static void EmitPathAndQuery(
        CodeBuilder source,
        HttpOperationModel operation,
        ClientMethodVariableNames names)
    {
        source
            .Append("var ")
            .Append(names.PathValues)
            .AppendLine(" = new global::System.Collections.Generic.Dictionary<string, object?>(global::System.StringComparer.OrdinalIgnoreCase)")
            .OpenBrace();

        foreach (var parameter in operation.Parameters.Where(parameter => parameter.Kind == BindKind.Path))
        {
            source
                .Append("[\"")
                .Append(Escape(parameter.Name))
                .Append("\"] = ")
                .Append(parameter.Symbol.Name)
                .AppendLine(",");
        }

        source
            .CloseBrace(appendSemicolon: true)
            .Append("var ")
            .Append(names.Query)
            .AppendLine(" = new global::System.Collections.Generic.List<string>();");

        foreach (var parameter in operation.Parameters.Where(parameter => parameter.Kind == BindKind.Query))
        {
            if (IsComplexQuery(parameter))
            {
                foreach (var property in GetQueryProperties(parameter))
                {
                    EmitQueryValue(
                        source,
                        property.Name,
                        parameter.Symbol.Name + "?." + property.Name,
                        names.Query);
                }
            }
            else
            {
                EmitQueryValue(source, parameter.Name, parameter.Symbol.Name, names.Query);
            }
        }

        source
            .Append("var ")
            .Append(names.Uri)
            .Append(" = global::Ling.RemoteServices.Client.RemoteServiceClientRuntime.BuildPath(\"")
            .Append(Escape(operation.FullRoute))
            .Append("\", ")
            .Append(names.PathValues)
            .AppendLine(");")
            .Append("if (")
            .Append(names.Query)
            .AppendLine(".Count != 0)")
            .OpenBrace()
            .Append(names.Uri)
            .Append(" += \"?\" + string.Join(\"&\", ")
            .Append(names.Query)
            .AppendLine(");")
            .CloseBrace();
    }

    private static void EmitQueryValue(
        CodeBuilder source,
        string name,
        string expression,
        string queryVariable)
    {
        source
            .Append("global::Ling.RemoteServices.Client.RemoteServiceClientRuntime.AddQuery(")
            .Append(queryVariable)
            .Append(", \"")
            .Append(Escape(name))
            .Append("\", ")
            .Append(expression)
            .AppendLine(");");
    }

    private static void EmitRequest(
        CodeBuilder source,
        HttpOperationModel operation,
        ClientMethodVariableNames names)
    {
        source
            .Append("using var ")
            .Append(names.Request)
            .Append(" = new global::System.Net.Http.HttpRequestMessage(new global::System.Net.Http.HttpMethod(\"")
            .Append(operation.Verb)
            .Append("\"), ")
            .Append(names.Uri)
            .AppendLine(");");

        foreach (var parameter in operation.Parameters.Where(parameter => parameter.Kind == BindKind.Header))
        {
            source
                .Append(names.Request)
                .Append(".Headers.TryAddWithoutValidation(\"")
                .Append(Escape(parameter.Name))
                .Append("\", global::Ling.RemoteServices.Client.RemoteServiceClientRuntime.Format(")
                .Append(parameter.Symbol.Name)
                .AppendLine("));");
        }

        var body = operation.Parameters.FirstOrDefault(parameter => parameter.Kind == BindKind.Body);
        if (body is not null)
        {
            source
                .Append(names.Request)
                .Append(".Content = global::System.Net.Http.Json.JsonContent.Create(")
                .Append(body.Symbol.Name)
                .Append(", global::Ling.RemoteServices.Client.RemoteServiceClientRuntime.GetJsonTypeInfo<")
                .Append(TypeName(body.Symbol.Type))
                .AppendLine(">(this.options.JsonSerializerOptions));");
        }

        EmitFormContent(source, operation, names);
    }

    private static void EmitFormContent(
        CodeBuilder source,
        HttpOperationModel operation,
        ClientMethodVariableNames names)
    {
        var formParameters = operation.Parameters
            .Where(parameter => parameter.Kind == BindKind.Form)
            .ToList();
        if (formParameters.Count == 0)
        {
            return;
        }

        source
            .Append("var ")
            .Append(names.Multipart)
            .AppendLine(" = new global::System.Net.Http.MultipartFormDataContent();")
            .Append(names.Request)
            .Append(".Content = ")
            .Append(names.Multipart)
            .AppendLine(";");

        foreach (var parameter in formParameters)
        {
            if (parameter.Symbol.Type.ToDisplayString()
                == "Ling.RemoteServices.Models.RemoteUploadFile")
            {
                EmitUploadFile(source, parameter, names);
                continue;
            }

            source
                .Append(names.Multipart)
                .Append(".Add(new global::System.Net.Http.StringContent(global::Ling.RemoteServices.Client.RemoteServiceClientRuntime.Format(")
                .Append(parameter.Symbol.Name)
                .Append(")), \"")
                .Append(Escape(parameter.Name))
                .AppendLine("\");");
        }
    }

    private static void EmitUploadFile(
        CodeBuilder source,
        ParameterModel parameter,
        ClientMethodVariableNames names)
    {
        var parameterName = parameter.Symbol.Name;
        var contentVariable = names.Create(parameterName + "Content");

        source
            .Append("var ")
            .Append(contentVariable)
            .Append(" = new global::System.Net.Http.StreamContent(")
            .Append(parameterName)
            .AppendLine(".Content);")
            .Append("if (!string.IsNullOrWhiteSpace(")
            .Append(parameterName)
            .AppendLine(".ContentType))")
            .OpenBrace()
            .Append(contentVariable)
            .Append(".Headers.ContentType = new global::System.Net.Http.Headers.MediaTypeHeaderValue(")
            .Append(parameterName)
            .AppendLine(".ContentType);")
            .CloseBrace()
            .Append(names.Multipart)
            .Append(".Add(")
            .Append(contentVariable)
            .Append(", \"")
            .Append(Escape(parameter.Name))
            .Append("\", ")
            .Append(parameterName)
            .AppendLine(".FileName);");
    }

    private static void EmitSend(
        CodeBuilder source,
        MethodModel method,
        HttpOperationModel operation,
        INamedTypeSymbol service,
        string cancellationToken,
        bool returnsFile,
        ClientMethodVariableNames names)
    {
        var completionOption = returnsFile
            ? "ResponseHeadersRead"
            : "ResponseContentRead";

        source
            .Append("var ")
            .Append(names.Response)
            .Append(" = await this.http.SendAsync(")
            .Append(names.Request)
            .Append(", global::System.Net.Http.HttpCompletionOption.")
            .Append(completionOption)
            .Append(", ")
            .Append(cancellationToken)
            .AppendLine(").ConfigureAwait(false);")
            .Append("if (!")
            .Append(names.Response)
            .AppendLine(".IsSuccessStatusCode)")
            .OpenBrace()
            .AppendLine("try")
            .OpenBrace()
            .Append("await global::Ling.RemoteServices.Client.RemoteServiceClientRuntime.ThrowForFailureAsync(")
            .Append(names.Response)
            .Append(", \"")
            .Append(Escape(service.Name + "." + method.Symbol.Name + "[" + operation.Verb + "]"))
            .Append("\", this.options, ")
            .Append(cancellationToken)
            .AppendLine(").ConfigureAwait(false);")
            .CloseBrace()
            .AppendLine("finally")
            .OpenBrace()
            .Append(names.Response)
            .AppendLine(".Dispose();")
            .CloseBrace()
            .CloseBrace();
    }

    private static void EmitResponse(
        CodeBuilder source,
        MethodModel method,
        HttpOperationModel operation,
        string cancellationToken,
        bool returnsFile,
        ClientMethodVariableNames names)
    {
        if (returnsFile)
        {
            source
                .Append("var ")
                .Append(names.Stream)
                .Append(" = await ")
                .Append(names.Response)
                .AppendLine(".Content.ReadAsStreamAsync().ConfigureAwait(false);")
                .AppendLine("return new global::Ling.RemoteServices.Models.RemoteFile(")
                .IncreaseIndentLevel()
                .Append(names.Stream)
                .AppendLine(",")
                .Append(names.Response)
                .AppendLine(".Content.Headers.ContentDisposition?.FileNameStar ?? ")
                .IncreaseIndentLevel()
                .Append(names.Response)
                .AppendLine(".Content.Headers.ContentDisposition?.FileName,")
                .DecreaseIndentLevel()
                .Append(names.Response)
                .AppendLine(".Content.Headers.ContentType?.MediaType,")
                .Append(names.Response)
                .AppendLine(".Content.Headers.ContentLength,")
                .Append(names.Response)
                .AppendLine(".Content.Headers.LastModified,")
                .Append(names.Response)
                .AppendLine(".Headers.ETag?.Tag,")
                .Append("global::Ling.RemoteServices.Client.RemoteServiceClientRuntime.Headers(")
                .Append(names.Response)
                .AppendLine("),")
                .Append("owner: ")
                .Append(names.Response)
                .AppendLine(");")
                .DecreaseIndentLevel();
            return;
        }

        if (method.Result is null)
        {
            source
                .Append(names.Response)
                .AppendLine(".Dispose();")
                .AppendLine("return;");
            return;
        }

        var isText = operation.ResponseContentType?.StartsWith("text/", StringComparison.OrdinalIgnoreCase) == true
            && method.Result.SpecialType == SpecialType.System_String;
        if (isText)
        {
            source
                .Append("using (")
                .Append(names.Response)
                .AppendLine(")")
                .OpenBrace()
                .Append("return await ")
                .Append(names.Response)
                .Append(".Content.ReadAsStringAsync(")
                .Append(cancellationToken)
                .AppendLine(").ConfigureAwait(false);")
                .CloseBrace();
            return;
        }

        var isByteArray = operation.ResponseContentType is not null
            && method.Result is IArrayTypeSymbol arrayType
            && arrayType.ElementType.SpecialType == SpecialType.System_Byte;
        if (isByteArray)
        {
            source
                .Append("using (")
                .Append(names.Response)
                .AppendLine(")")
                .OpenBrace()
                .Append("return await ")
                .Append(names.Response)
                .Append(".Content.ReadAsByteArrayAsync(")
                .Append(cancellationToken)
                .AppendLine(").ConfigureAwait(false);")
                .CloseBrace();
            return;
        }

        source
            .Append("using (")
            .Append(names.Response)
            .AppendLine(")")
            .OpenBrace()
            .Append("return (await global::System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<")
            .Append(TypeName(method.Result))
            .Append(">(")
            .Append(names.Response)
            .Append(".Content, global::Ling.RemoteServices.Client.RemoteServiceClientRuntime.GetJsonTypeInfo<")
            .Append(TypeName(method.Result))
            .Append(">(this.options.JsonSerializerOptions), ")
            .Append(cancellationToken)
            .AppendLine(").ConfigureAwait(false))!;")
            .CloseBrace();
    }

    private static void EmitExceptionHandling(
        CodeBuilder source,
        MethodModel method,
        INamedTypeSymbol service)
    {
        source
            .CloseBrace()
            .AppendLine("catch (global::System.OperationCanceledException)")
            .OpenBrace()
            .AppendLine("throw;")
            .CloseBrace()
            .AppendLine("catch (global::Ling.RemoteServices.Exceptions.RemoteCallException)")
            .OpenBrace()
            .AppendLine("throw;")
            .CloseBrace()
            .AppendLine("catch (global::System.Exception exception)")
            .OpenBrace()
            .Append("throw new global::Ling.RemoteServices.Exceptions.RemoteTransportException(\"")
            .Append(Escape(service.Name + "." + method.Symbol.Name))
            .AppendLine("\", null, exception);")
            .CloseBrace()
            .CloseBrace()
            .AppendLine();
    }

}
