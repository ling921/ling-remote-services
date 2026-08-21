namespace Ling.RemoteServices.AspNetCore;

/// <summary>
/// Configures host-defined endpoint convention policies used by remote service contracts.
/// </summary>
public sealed class RemoteServiceOptions
{
    private readonly Dictionary<string, Action<RemoteServiceEndpointPolicyContext>> endpointPolicies =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Registers a named endpoint convention policy.
    /// </summary>
    /// <param name="policyName">The name referenced by a remote endpoint policy attribute.</param>
    /// <param name="configure">The endpoint convention callback.</param>
    /// <returns>The current options instance.</returns>
    /// <exception cref="ArgumentException">The policy name has already been registered.</exception>
    public RemoteServiceOptions AddEndpointPolicy(
        string policyName,
        Action<RemoteServiceEndpointPolicyContext> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ArgumentNullException.ThrowIfNull(configure);

        return endpointPolicies.TryAdd(policyName, configure)
            ? this
            : throw new ArgumentException(
                $"A remote endpoint policy named '{policyName}' is already registered.",
                nameof(policyName));
    }

    internal bool TryGetEndpointPolicy(
        string policyName,
        out Action<RemoteServiceEndpointPolicyContext>? configure)
    {
        return endpointPolicies.TryGetValue(policyName, out configure);
    }
}
