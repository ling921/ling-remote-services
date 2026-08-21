namespace Ling.RemoteServices.NativeAotSmoke;

/// <summary>
/// Represents a JSON request used by the Native AOT smoke test.
/// </summary>
public sealed class AotEchoRequest
{
    /// <summary>
    /// Gets or sets the value to echo.
    /// </summary>
    public string? Value { get; set; }
}
