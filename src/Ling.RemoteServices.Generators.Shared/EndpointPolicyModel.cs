namespace Ling.RemoteServices.Generators;

internal sealed record EndpointPolicyModel(
    IReadOnlyList<string?> AuthorizationPolicyNames,
    bool AllowAnonymous,
    string? CorsPolicyName,
    bool OutputCacheEnabled,
    string? OutputCachePolicyName,
    string? RateLimitPolicyName,
    string? RequestTimeoutPolicyName,
    IReadOnlyList<string> CustomPolicyNames)
{
    public static EndpointPolicyModel Empty { get; } = new(
        Array.Empty<string?>(),
        false,
        null,
        false,
        null,
        null,
        null,
        Array.Empty<string>());
}
