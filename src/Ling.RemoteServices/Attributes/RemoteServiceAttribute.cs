using System.Diagnostics.CodeAnalysis;

namespace Ling.RemoteServices.Attributes;

/// <summary>
/// Indicates that the target interface is a remote service.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class RemoteServiceAttribute([StringSyntax("Route")] string route) : Attribute
{
    /// <summary>
    /// Gets the base route for the remote service.
    /// </summary>
    [StringSyntax("Route")]
    public string Route { get; } = route;
}

/// <summary>
/// Identifies a remote service contract in a generated assembly manifest.
/// </summary>
[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class RemoteServiceContractAttribute : Attribute
{
    /// <summary>
    /// Initializes a new contract manifest entry for generated infrastructure.
    /// </summary>
    /// <param name="serviceType">The remote service interface exposed by the assembly.</param>
    public RemoteServiceContractAttribute(Type serviceType)
    {
        ServiceType = serviceType;
    }

    /// <summary>
    /// Gets the remote service interface exposed by the assembly.
    /// </summary>
    public Type ServiceType { get; }
}
