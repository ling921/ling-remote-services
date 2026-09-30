namespace Ling.RemoteServices.Attributes;

/// <summary>
/// Applies a named request timeout policy to a generated remote service endpoint.
/// </summary>
[AttributeUsage(
    AttributeTargets.Interface | AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
public sealed class RemoteRequestTimeoutAttribute : RemoteEndpointMetadataAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteRequestTimeoutAttribute"/> class.
    /// </summary>
    /// <param name="policyName">The request timeout policy name defined by the host.</param>
    public RemoteRequestTimeoutAttribute(string policyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        PolicyName = policyName;
    }

    /// <summary>Initializes a request timeout with a positive duration in milliseconds.</summary>
    public RemoteRequestTimeoutAttribute(int timeoutMilliseconds)
    {
        if (timeoutMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));
        }

        TimeoutMilliseconds = timeoutMilliseconds;
    }

    /// <summary>
    /// Gets the request timeout policy name.
    /// </summary>
    public string? PolicyName { get; }

    /// <summary>Gets the timeout duration when configured directly.</summary>
    public int TimeoutMilliseconds { get; }
}
