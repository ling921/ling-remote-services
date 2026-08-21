namespace Ling.RemoteServices.Attributes;

/// <summary>
/// Applies a named host-defined endpoint convention policy to a generated endpoint.
/// </summary>
[AttributeUsage(
    AttributeTargets.Interface | AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = true)]
public sealed class RemoteEndpointPolicyAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteEndpointPolicyAttribute"/> class.
    /// </summary>
    /// <param name="policyName">The custom endpoint policy name defined by the host.</param>
    public RemoteEndpointPolicyAttribute(string policyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        PolicyName = policyName;
    }

    /// <summary>
    /// Gets the custom endpoint policy name.
    /// </summary>
    public string PolicyName { get; }
}
