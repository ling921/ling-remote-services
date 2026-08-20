namespace Ling.RemoteServices.Attributes;

/// <summary>
/// Indicates that the target interface is a remote service.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class RemoteServiceAttribute : Attribute
{
    /// <summary>
    /// Gets the base route for the remote service.
    /// </summary>
    public string? Route { get; }
}
