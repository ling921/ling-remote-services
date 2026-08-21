namespace Ling.RemoteServices.Generators;

internal sealed record EndpointPolicyModel(
    IReadOnlyList<string?> AuthorizationPolicyNames,
    IReadOnlyList<string> AuthorizationRoleGroups,
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
        Array.Empty<string>(),
        false,
        null,
        false,
        null,
        null,
        null,
        Array.Empty<string>());

    public bool HasAuthorization =>
        AuthorizationPolicyNames.Count > 0 || AuthorizationRoleGroups.Count > 0;
}
