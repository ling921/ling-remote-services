namespace Ling.RemoteServices.Attributes;

/// <summary>
/// Requires authorization for a generated remote service endpoint.
/// </summary>
[AttributeUsage(
    AttributeTargets.Interface | AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = true)]
public sealed class RemoteAuthorizeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteAuthorizeAttribute"/> class
    /// that uses the host's default authorization policy.
    /// </summary>
    public RemoteAuthorizeAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteAuthorizeAttribute"/> class.
    /// </summary>
    /// <param name="policyName">The authorization policy name defined by the host.</param>
    public RemoteAuthorizeAttribute(string policyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        PolicyName = policyName;
    }

    /// <summary>
    /// Gets the authorization policy name, or <see langword="null"/> to use the default policy.
    /// </summary>
    public string? PolicyName { get; }
}
