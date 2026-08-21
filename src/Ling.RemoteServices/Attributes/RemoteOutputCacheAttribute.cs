namespace Ling.RemoteServices.Attributes;

/// <summary>
/// Enables ASP.NET Core output caching for a generated remote service endpoint.
/// </summary>
[AttributeUsage(
    AttributeTargets.Interface | AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
public sealed class RemoteOutputCacheAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteOutputCacheAttribute"/> class
    /// that uses the host's default output cache policy.
    /// </summary>
    public RemoteOutputCacheAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteOutputCacheAttribute"/> class.
    /// </summary>
    /// <param name="policyName">The output cache policy name defined by the host.</param>
    public RemoteOutputCacheAttribute(string policyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        PolicyName = policyName;
    }

    /// <summary>
    /// Gets the output cache policy name, or <see langword="null"/> to use the default policy.
    /// </summary>
    public string? PolicyName { get; }
}
