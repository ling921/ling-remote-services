namespace Ling.RemoteServices.Generators.Testing;

/// <summary>
/// Provides representative contracts shared by source generator tests.
/// </summary>
public static class GeneratorTestSources
{
    /// <summary>
    /// Gets a contract containing two services and endpoint policy metadata.
    /// </summary>
    public const string Contracts = """
        using Ling.RemoteServices.Attributes;
        using System.Threading.Tasks;

        namespace GeneratorFixtures;

        [RemoteService("/api/first")]
        [RemoteAuthorize("ApiUser")]
        [RemoteCors("Frontend")]
        [RemoteEndpointPolicy("ServicePolicy")]
        public interface IFirstService
        {
            /// <summary>Gets the first value.</summary>
            [Get(IsClientDefault = true), Post]
            [RemoteAllowAnonymous]
            [RemoteOutputCache("Weather")]
            [RemoteRateLimit("Reads")]
            [RemoteRequestTimeout("Fast")]
            [RemoteEndpointPolicy("MethodPolicy")]
            Task<string> GetAsync();
        }

        [RemoteService("/api/second")]
        public interface ISecondService
        {
            [Get("items")]
            Task<string[]> GetItemsAsync();

            [Post("items/{id}")]
            Task<string> UpdateAsync(
                [Path] int id,
                [Query] string? tag,
                [Header("X-Request-ID")] System.Guid requestId,
                [Body] UpdateRequest request,
                System.Threading.CancellationToken cancellationToken = default);

            [Post("upload")]
            Task UploadAsync(
                [Form("file")] Ling.RemoteServices.Models.RemoteUploadFile file,
                System.Threading.CancellationToken cancellationToken = default);
        }

        public sealed class UpdateRequest
        {
            public string? Value { get; set; }
        }
        """;
}
