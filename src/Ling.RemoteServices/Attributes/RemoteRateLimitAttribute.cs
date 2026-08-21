namespace Ling.RemoteServices.Attributes;

/// <summary>
/// Applies a named rate limiting policy to a generated remote service endpoint.
/// </summary>
[AttributeUsage(
    AttributeTargets.Interface | AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
public sealed class RemoteRateLimitAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteRateLimitAttribute"/> class.
    /// </summary>
    /// <param name="policyName">The rate limiting policy name defined by the host.</param>
    public RemoteRateLimitAttribute(string policyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        PolicyName = policyName;
    }

    /// <summary>
    /// Gets the rate limiting policy name.
    /// </summary>
    public string PolicyName { get; }
}
