using Microsoft.CodeAnalysis;

namespace Ling.RemoteServices.Generators;

internal static class EndpointPolicyParser
{
    public static EndpointPolicyModel ParseEffective(
        INamedTypeSymbol service,
        IMethodSymbol method,
        Action<Diagnostic>? reportDiagnostic)
    {
        var servicePolicies = ParseDeclared(service, reportDiagnostic);
        var methodPolicies = ParseDeclared(method, reportDiagnostic);

        var authorizationPolicies = servicePolicies.AuthorizationPolicyNames
            .Concat(methodPolicies.AuthorizationPolicyNames)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var allowAnonymous = methodPolicies.AllowAnonymous
            || (servicePolicies.AllowAnonymous
                && methodPolicies.AuthorizationPolicyNames.Count == 0);

        return new EndpointPolicyModel(
            authorizationPolicies,
            allowAnonymous,
            methodPolicies.CorsPolicyName ?? servicePolicies.CorsPolicyName,
            methodPolicies.OutputCacheEnabled || servicePolicies.OutputCacheEnabled,
            methodPolicies.OutputCacheEnabled
                ? methodPolicies.OutputCachePolicyName
                : servicePolicies.OutputCachePolicyName,
            methodPolicies.RateLimitPolicyName ?? servicePolicies.RateLimitPolicyName,
            methodPolicies.RequestTimeoutPolicyName ?? servicePolicies.RequestTimeoutPolicyName,
            servicePolicies.CustomPolicyNames
                .Concat(methodPolicies.CustomPolicyNames)
                .Distinct(StringComparer.Ordinal)
                .ToArray());
    }

    private static EndpointPolicyModel ParseDeclared(
        ISymbol symbol,
        Action<Diagnostic>? reportDiagnostic)
    {
        var authorizationPolicies = new List<string?>();
        var allowAnonymous = false;
        string? corsPolicyName = null;
        var outputCacheEnabled = false;
        string? outputCachePolicyName = null;
        string? rateLimitPolicyName = null;
        string? requestTimeoutPolicyName = null;
        var customPolicyNames = new List<string>();

        foreach (var attribute in symbol.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();
            switch (attributeName)
            {
                case ContractNames.AuthorizeAttribute:
                    if (TryGetOptionalPolicyName(attribute, symbol, reportDiagnostic, out var authorizationPolicy))
                    {
                        authorizationPolicies.Add(authorizationPolicy);
                    }

                    break;
                case ContractNames.AllowAnonymousAttribute:
                    allowAnonymous = true;
                    break;
                case ContractNames.CorsAttribute:
                    corsPolicyName = GetRequiredPolicyName(attribute, symbol, reportDiagnostic);
                    break;
                case ContractNames.OutputCacheAttribute:
                    if (TryGetOptionalPolicyName(attribute, symbol, reportDiagnostic, out var outputCachePolicy))
                    {
                        outputCacheEnabled = true;
                        outputCachePolicyName = outputCachePolicy;
                    }

                    break;
                case ContractNames.RateLimitAttribute:
                    rateLimitPolicyName = GetRequiredPolicyName(attribute, symbol, reportDiagnostic);
                    break;
                case ContractNames.RequestTimeoutAttribute:
                    requestTimeoutPolicyName = GetRequiredPolicyName(attribute, symbol, reportDiagnostic);
                    break;
                case ContractNames.EndpointPolicyAttribute:
                    var customPolicyName = GetRequiredPolicyName(attribute, symbol, reportDiagnostic);
                    if (customPolicyName is not null)
                    {
                        customPolicyNames.Add(customPolicyName);
                    }

                    break;
            }
        }

        return new EndpointPolicyModel(
            authorizationPolicies,
            allowAnonymous,
            corsPolicyName,
            outputCacheEnabled,
            outputCachePolicyName,
            rateLimitPolicyName,
            requestTimeoutPolicyName,
            customPolicyNames);
    }

    private static bool TryGetOptionalPolicyName(
        AttributeData attribute,
        ISymbol symbol,
        Action<Diagnostic>? reportDiagnostic,
        out string? policyName)
    {
        if (attribute.ConstructorArguments.Length == 0)
        {
            policyName = null;
            return true;
        }

        policyName = attribute.ConstructorArguments[0].Value as string;
        if (!string.IsNullOrWhiteSpace(policyName))
        {
            return true;
        }

        ReportInvalidPolicyName(attribute, symbol, reportDiagnostic);
        return false;
    }

    private static string? GetRequiredPolicyName(
        AttributeData attribute,
        ISymbol symbol,
        Action<Diagnostic>? reportDiagnostic)
    {
        var policyName = attribute.ConstructorArguments.FirstOrDefault().Value as string;
        if (!string.IsNullOrWhiteSpace(policyName))
        {
            return policyName;
        }

        ReportInvalidPolicyName(attribute, symbol, reportDiagnostic);
        return null;
    }

    private static void ReportInvalidPolicyName(
        AttributeData attribute,
        ISymbol symbol,
        Action<Diagnostic>? reportDiagnostic)
    {
        reportDiagnostic?.Invoke(Diagnostic.Create(
            ContractDiagnostics.Invalid,
            attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                ?? symbol.Locations.FirstOrDefault(),
            $"Endpoint policy attribute '{attribute.AttributeClass?.Name}' on "
            + $"'{symbol.Name}' requires a non-empty policy name."));
    }
}
