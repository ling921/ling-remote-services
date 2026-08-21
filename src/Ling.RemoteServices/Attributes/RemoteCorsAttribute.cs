namespace Ling.RemoteServices.Attributes;

/// <summary>
/// Applies a named CORS policy to a generated remote service endpoint.
/// </summary>
[AttributeUsage(
    AttributeTargets.Interface | AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
public sealed class RemoteCorsAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteCorsAttribute"/> class.
    /// </summary>
    /// <param name="policyName">The CORS policy name defined by the host.</param>
    public RemoteCorsAttribute(string policyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        PolicyName = policyName;
    }

    /// <summary>
    /// Gets the CORS policy name.
    /// </summary>
    public string PolicyName { get; }
}
