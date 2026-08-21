using Ling.RemoteServices.Attributes;

namespace Ling.RemoteServices.NativeAotSmoke;

/// <summary>
/// Defines the contract compiled by the Native AOT smoke test.
/// </summary>
[RemoteService("/aot")]
public interface IAotEchoService
{
    /// <summary>
    /// Echoes a JSON request through a generated endpoint and client proxy.
    /// </summary>
    /// <param name="id">The route identifier.</param>
    /// <param name="tag">An optional query tag.</param>
    /// <param name="requestId">The request identifier supplied in a header.</param>
    /// <param name="request">The request to echo.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The echoed response.</returns>
    [Post("echo/{id}")]
    Task<AotEchoResponse> EchoAsync(
        [Path] int id,
        [Query] string? tag,
        [Header("X-Request-ID")] Guid requestId,
        [Body] AotEchoRequest request,
        CancellationToken cancellationToken = default);
}
