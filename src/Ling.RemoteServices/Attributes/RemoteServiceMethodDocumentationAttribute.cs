using System.ComponentModel;

namespace Ling.RemoteServices.Attributes;

/// <summary>
/// Preserves remote service method documentation in the generated assembly manifest.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class RemoteServiceMethodDocumentationAttribute : Attribute
{
    /// <summary>
    /// Initializes a new remote service method documentation entry.
    /// </summary>
    /// <param name="serviceType">The remote service contract type.</param>
    /// <param name="methodName">The remote service method name.</param>
    /// <param name="summary">The method summary.</param>
    public RemoteServiceMethodDocumentationAttribute(
        Type serviceType,
        string methodName,
        string summary)
    {
        ServiceType = serviceType;
        MethodName = methodName;
        Summary = summary;
    }

    /// <summary>
    /// Gets the remote service contract type.
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// Gets the remote service method name.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Gets the method summary.
    /// </summary>
    public string Summary { get; }
}
