using System.ComponentModel;
using Microsoft.AspNetCore.Antiforgery;

namespace Ling.RemoteServices.AspNetCore;

/// <summary>
/// Marks generated form endpoints as requiring antiforgery validation.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RemoteServiceAntiforgeryMetadata : IAntiforgeryMetadata
{
    private RemoteServiceAntiforgeryMetadata()
    {
    }

    /// <summary>
    /// Gets the shared metadata instance for generated form endpoints.
    /// </summary>
    public static RemoteServiceAntiforgeryMetadata Required { get; } = new();

    /// <inheritdoc />
    public bool RequiresValidation => true;
}
