using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ling.RemoteServices.AspNetCore;

/// <summary>
/// Applies generated remote service policy metadata to ASP.NET Core endpoint builders.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class RemoteServiceEndpointPolicyRuntime
{
    /// <summary>
    /// Applies built-in and host-defined policies to a generated endpoint.
    /// </summary>
    /// <param name="services">The host service provider.</param>
    /// <param name="endpoint">The generated endpoint convention builder.</param>
    /// <param name="serviceType">The remote service contract type.</param>
    /// <param name="methodName">The remote contract method name.</param>
    /// <param name="metadata">The policy metadata emitted from the contract.</param>
    public static void Apply(
        IServiceProvider services,
        IEndpointConventionBuilder endpoint,
        Type serviceType,
        string methodName,
        RemoteServiceEndpointPolicyMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        ArgumentNullException.ThrowIfNull(metadata);

        foreach (var policyName in metadata.AuthorizationPolicyNames)
        {
            if (policyName is null)
            {
                endpoint.RequireAuthorization();
            }
            else
            {
                endpoint.RequireAuthorization(policyName);
            }
        }

        foreach (var roles in metadata.AuthorizationRoleGroups)
        {
            endpoint.RequireAuthorization(new AuthorizeAttribute { Roles = roles });
        }

        if (metadata.AllowAnonymous)
        {
            endpoint.AllowAnonymous();
        }

        if (metadata.CorsPolicyName is not null)
        {
            endpoint.RequireCors(metadata.CorsPolicyName);
        }

        if (metadata.OutputCacheEnabled)
        {
            if (metadata.OutputCachePolicyName is null)
            {
                endpoint.CacheOutput();
            }
            else
            {
                endpoint.CacheOutput(metadata.OutputCachePolicyName);
            }
        }

        if (metadata.RateLimitPolicyName is not null)
        {
            endpoint.RequireRateLimiting(metadata.RateLimitPolicyName);
        }

        if (metadata.RequestTimeoutPolicyName is not null)
        {
            endpoint.WithRequestTimeout(metadata.RequestTimeoutPolicyName);
        }

        ApplyCustomPolicies(services, endpoint, serviceType, methodName, metadata.CustomPolicyNames);
    }

    private static void ApplyCustomPolicies(
        IServiceProvider services,
        IEndpointConventionBuilder endpoint,
        Type serviceType,
        string methodName,
        IReadOnlyList<string> policyNames)
    {
        if (policyNames.Count == 0)
        {
            return;
        }

        var options = services.GetService<IOptions<RemoteServiceOptions>>()?.Value
            ?? throw new InvalidOperationException(
                "Remote endpoint policies are used by a contract, but remote services "
                + "were not registered. Call AddRemoteServices() during service registration.");
        var context = new RemoteServiceEndpointPolicyContext(serviceType, methodName, endpoint);

        foreach (var policyName in policyNames)
        {
            if (!options.TryGetEndpointPolicy(policyName, out var configure)
                || configure is null)
            {
                throw new InvalidOperationException(
                    $"Remote endpoint policy '{policyName}' used by "
                    + $"'{serviceType.FullName}.{methodName}' is not registered.");
            }

            configure(context);
        }
    }
}
