namespace Ling.RemoteServices.Attributes;

/// <summary>
/// Allows anonymous access to a generated remote service endpoint.
/// </summary>
[AttributeUsage(
    AttributeTargets.Interface | AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
public sealed class RemoteAllowAnonymousAttribute : Attribute
{
}
