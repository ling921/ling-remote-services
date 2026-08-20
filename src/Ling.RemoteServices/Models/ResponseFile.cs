namespace Ling.RemoteServices.Models;

/// <summary>
/// Represents a response file.
/// </summary>
/// <param name="file">The file stream.</param>
/// <param name="fileName">The file name.</param>
/// <param name="contentType">The content type. e.g. "application/pdf"</param>
/// <param name="headers">The headers. Optional.</param>
public sealed class ResponseFile(
    Stream file,
    string fileName,
    string contentType,
    IDictionary<string, string>? headers = null)
{
    /// <summary>
    /// Gets the file stream.
    /// </summary>
    public Stream File { get; } = file;

    /// <summary>
    /// Gets the file name.
    /// </summary>
    public string FileName { get; } = fileName;

    /// <summary>
    /// Gets the content type.
    /// </summary>
    public string ContentType { get; } = contentType;

    /// <summary>
    /// Gets the headers.
    /// </summary>
    public IDictionary<string, string>? Headers { get; } = headers;
}
