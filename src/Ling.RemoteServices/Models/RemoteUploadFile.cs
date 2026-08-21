namespace Ling.RemoteServices.Models;

/// <summary>
/// Describes a file supplied to a remote service operation.
/// </summary>
public sealed class RemoteUploadFile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteUploadFile"/> class.
    /// </summary>
    /// <param name="content">The stream containing the file data.</param>
    /// <param name="fileName">The file name sent with the request.</param>
    /// <param name="contentType">The file media type, or <see langword="null"/> when unspecified.</param>
    /// <param name="length">The file length, or <see langword="null"/> when unknown.</param>
    /// <param name="leaveOpen">Whether the caller retains ownership of <paramref name="content"/>.</param>
    public RemoteUploadFile(
        Stream content,
        string fileName,
        string? contentType = null,
        long? length = null,
        bool leaveOpen = true)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        Content = content;
        FileName = fileName;
        ContentType = contentType;
        Length = length;
        LeaveOpen = leaveOpen;
    }

    /// <summary>Gets the stream containing the file data.</summary>
    public Stream Content { get; }

    /// <summary>Gets the file name sent with the request.</summary>
    public string FileName { get; }

    /// <summary>Gets the file media type, or <see langword="null"/> when unspecified.</summary>
    public string? ContentType { get; }

    /// <summary>Gets the file length, or <see langword="null"/> when unknown.</summary>
    public long? Length { get; }

    /// <summary>Gets a value indicating whether the caller retains ownership of <see cref="Content"/>.</summary>
    public bool LeaveOpen { get; }
}
