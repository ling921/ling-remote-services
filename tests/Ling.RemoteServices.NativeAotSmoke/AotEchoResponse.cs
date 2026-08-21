namespace Ling.RemoteServices.NativeAotSmoke;

/// <summary>
/// Represents a JSON response used by the Native AOT smoke test.
/// </summary>
public sealed class AotEchoResponse
{
    /// <summary>
    /// Gets or sets the route identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the optional query tag.
    /// </summary>
    public string? Tag { get; set; }

    /// <summary>
    /// Gets or sets the request identifier supplied in a header.
    /// </summary>
    public Guid RequestId { get; set; }

    /// <summary>
    /// Gets or sets the echoed value.
    /// </summary>
    public string? Value { get; set; }
}
