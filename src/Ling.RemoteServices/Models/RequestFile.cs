namespace Ling.RemoteServices.Models;

/// <summary>
/// Represents a request file.
/// </summary>
/// <param name="file">The file stream.</param>
/// <param name="fileName">The file name.</param>
/// <param name="description">The file description. Optional.</param>
public sealed class RequestFile(
    Stream file,
    string fileName,
    string? description = null)
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
    /// Gets the file description.
    /// </summary>
    public string? Description { get; } = description;
}
