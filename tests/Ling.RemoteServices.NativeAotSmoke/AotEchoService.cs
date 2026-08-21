namespace Ling.RemoteServices.NativeAotSmoke;

/// <summary>
/// Implements the Native AOT smoke-test contract.
/// </summary>
public sealed class AotEchoService : IAotEchoService
{
    /// <inheritdoc />
    public Task<AotEchoResponse> EchoAsync(
        int id,
        string? tag,
        Guid requestId,
        AotEchoRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AotEchoResponse
        {
            Id = id,
            Tag = tag,
            RequestId = requestId,
            Value = request.Value
        });
    }
}
