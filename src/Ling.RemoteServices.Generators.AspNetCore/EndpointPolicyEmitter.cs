using static Ling.RemoteServices.Generators.GeneratorUtilities;

namespace Ling.RemoteServices.Generators;

internal static class EndpointPolicyEmitter
{
    public static void EmitApply(
        CodeBuilder source,
        ServiceModel service,
        MethodModel method,
        string operationVariable)
    {
        var policies = method.EndpointPolicies;
        if (!HasPolicies(policies))
        {
            return;
        }

        source
            .AppendLine("global::Ling.RemoteServices.AspNetCore.RemoteServiceEndpointPolicyRuntime.Apply(")
            .IncreaseIndentLevel()
            .AppendLine("endpoints.ServiceProvider,")
            .Append(operationVariable)
            .AppendLine(",")
            .Append("typeof(")
            .Append(TypeName(service.Symbol))
            .AppendLine("),")
            .Append("\"")
            .Append(Escape(method.Symbol.Name))
            .AppendLine("\",")
            .AppendLine("new global::Ling.RemoteServices.AspNetCore.RemoteServiceEndpointPolicyMetadata")
            .OpenBrace();

        EmitAuthorizationPolicies(source, policies.AuthorizationPolicyNames);

        if (policies.AllowAnonymous)
        {
            source.AppendLine("AllowAnonymous = true,");
        }

        EmitStringProperty(source, "CorsPolicyName", policies.CorsPolicyName);

        if (policies.OutputCacheEnabled)
        {
            source.AppendLine("OutputCacheEnabled = true,");
            EmitStringProperty(source, "OutputCachePolicyName", policies.OutputCachePolicyName);
        }

        EmitStringProperty(source, "RateLimitPolicyName", policies.RateLimitPolicyName);
        EmitStringProperty(source, "RequestTimeoutPolicyName", policies.RequestTimeoutPolicyName);
        EmitCustomPolicies(source, policies.CustomPolicyNames);

        source
            .CloseBrace(");")
            .DecreaseIndentLevel();
    }

    private static bool HasPolicies(EndpointPolicyModel policies)
    {
        return policies.AuthorizationPolicyNames.Count > 0
            || policies.AllowAnonymous
            || policies.CorsPolicyName is not null
            || policies.OutputCacheEnabled
            || policies.RateLimitPolicyName is not null
            || policies.RequestTimeoutPolicyName is not null
            || policies.CustomPolicyNames.Count > 0;
    }

    private static void EmitAuthorizationPolicies(
        CodeBuilder source,
        IReadOnlyList<string?> policyNames)
    {
        if (policyNames.Count == 0)
        {
            return;
        }

        source.Append("AuthorizationPolicyNames = new string?[] { ");
        for (var index = 0; index < policyNames.Count; index++)
        {
            if (index > 0)
            {
                source.Append(", ");
            }

            if (policyNames[index] is { } policyName)
            {
                source
                    .Append('"')
                    .Append(Escape(policyName))
                    .Append('"');
            }
            else
            {
                source.Append("null");
            }
        }

        source.AppendLine(" },");
    }

    private static void EmitCustomPolicies(
        CodeBuilder source,
        IReadOnlyList<string> policyNames)
    {
        if (policyNames.Count == 0)
        {
            return;
        }

        source.Append("CustomPolicyNames = new string[] { ");
        for (var index = 0; index < policyNames.Count; index++)
        {
            if (index > 0)
            {
                source.Append(", ");
            }

            source
                .Append('"')
                .Append(Escape(policyNames[index]))
                .Append('"');
        }

        source.AppendLine(" },");
    }

    private static void EmitStringProperty(
        CodeBuilder source,
        string propertyName,
        string? value)
    {
        if (value is null)
        {
            return;
        }

        source
            .Append(propertyName)
            .Append(" = \"")
            .Append(Escape(value))
            .AppendLine("\",");
    }
}
