namespace Ling.RemoteServices.Models;

/// <summary>
/// Represents a streamed file returned by a remote service operation.
/// </summary>
public sealed class RemoteFile : IDisposable, IAsyncDisposable
{
    private readonly IDisposable? owner;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteFile"/> class.
    /// </summary>
    /// <param name="content">The stream containing the response data.</param>
    /// <param name="fileName">The suggested download file name.</param>
    /// <param name="contentType">The response media type.</param>
    /// <param name="contentLength">The response content length.</param>
    /// <param name="lastModified">The last-modified timestamp.</param>
    /// <param name="entityTag">The HTTP entity tag.</param>
    /// <param name="headers">The response headers.</param>
    /// <param name="enableRangeProcessing">Whether the server should process range requests.</param>
    /// <param name="owner">An additional resource disposed with this instance.</param>
    public RemoteFile(
        Stream content,
        string? fileName = null,
        string? contentType = null,
        long? contentLength = null,
        DateTimeOffset? lastModified = null,
        string? entityTag = null,
        IReadOnlyDictionary<string, string[]>? headers = null,
        bool enableRangeProcessing = false,
        IDisposable? owner = null)
    {
        ArgumentNullException.ThrowIfNull(content);

        Content = content;
        FileName = fileName;
        ContentType = contentType;
        ContentLength = contentLength;
        LastModified = lastModified;
        EntityTag = entityTag;
        Headers = headers ?? new Dictionary<string, string[]>();
        EnableRangeProcessing = enableRangeProcessing;
        this.owner = owner;
    }

    /// <summary>Gets the stream containing the response data.</summary>
    public Stream Content { get; }

    /// <summary>Gets the suggested download file name.</summary>
    public string? FileName { get; }

    /// <summary>Gets the response media type.</summary>
    public string? ContentType { get; }

    /// <summary>Gets the response content length.</summary>
    public long? ContentLength { get; }

    /// <summary>Gets the last-modified timestamp.</summary>
    public DateTimeOffset? LastModified { get; }

    /// <summary>Gets the HTTP entity tag.</summary>
    public string? EntityTag { get; }

    /// <summary>Gets the response headers.</summary>
    public IReadOnlyDictionary<string, string[]> Headers { get; }

    /// <summary>Gets a value indicating whether the server should process range requests.</summary>
    public bool EnableRangeProcessing { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        Content.Dispose();
        owner?.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Content.DisposeAsync().ConfigureAwait(false);
        owner?.Dispose();
    }
}
