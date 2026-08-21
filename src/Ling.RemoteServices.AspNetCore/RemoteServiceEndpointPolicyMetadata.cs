using System.ComponentModel;

namespace Ling.RemoteServices.AspNetCore;

/// <summary>
/// Describes endpoint policies emitted from remote service contract attributes.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RemoteServiceEndpointPolicyMetadata
{
    /// <summary>
    /// Gets or initializes the authorization policy names. A null entry selects the default policy.
    /// </summary>
    public IReadOnlyList<string?> AuthorizationPolicyNames { get; init; } = [];

    /// <summary>
    /// Gets or initializes a value indicating whether anonymous access is allowed.
    /// </summary>
    public bool AllowAnonymous { get; init; }

    /// <summary>
    /// Gets or initializes the named CORS policy.
    /// </summary>
    public string? CorsPolicyName { get; init; }

    /// <summary>
    /// Gets or initializes a value indicating whether output caching is enabled.
    /// </summary>
    public bool OutputCacheEnabled { get; init; }

    /// <summary>
    /// Gets or initializes the output cache policy name, or null for the default policy.
    /// </summary>
    public string? OutputCachePolicyName { get; init; }

    /// <summary>
    /// Gets or initializes the named rate limiting policy.
    /// </summary>
    public string? RateLimitPolicyName { get; init; }

    /// <summary>
    /// Gets or initializes the named request timeout policy.
    /// </summary>
    public string? RequestTimeoutPolicyName { get; init; }

    /// <summary>
    /// Gets or initializes the custom endpoint policy names.
    /// </summary>
    public IReadOnlyList<string> CustomPolicyNames { get; init; } = [];
}
