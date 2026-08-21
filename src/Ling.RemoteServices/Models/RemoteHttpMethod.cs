namespace Ling.RemoteServices;

/// <summary>
/// Identifies an HTTP method exposed by a remote service operation.
/// </summary>
public enum RemoteHttpMethod
{
    /// <summary>
    /// Represents the HTTP GET method.
    /// </summary>
    Get,

    /// <summary>
    /// Represents the HTTP POST method.
    /// </summary>
    Post,

    /// <summary>
    /// Represents the HTTP PUT method.
    /// </summary>
    Put,

    /// <summary>
    /// Represents the HTTP PATCH method.
    /// </summary>
    Patch,

    /// <summary>
    /// Represents the HTTP DELETE method.
    /// </summary>
    Delete
}
