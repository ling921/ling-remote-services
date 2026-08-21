using Ling.RemoteServices.Models;

namespace Ling.RemoteServices.Exceptions;

#pragma warning disable RCS1194 // Implement exception constructors

/// <summary>Represents an expected service error returned as Problem Details.</summary>
public class RemoteServiceException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="RemoteServiceException"/> class.</summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    /// <param name="problem">The Problem Details payload.</param>
    /// <param name="headers">Additional response headers.</param>
    public RemoteServiceException(
        int statusCode,
        RemoteProblemDetails? problem = null,
        IReadOnlyDictionary<string, string[]>? headers = null)
        : base(problem?.Detail ?? problem?.Title)
    {
        StatusCode = statusCode;
        Problem = problem;
        Headers = headers ?? new Dictionary<string, string[]>();
    }

    /// <summary>Gets the HTTP status code returned to the caller.</summary>
    public int StatusCode { get; }

    /// <summary>Gets the Problem Details payload.</summary>
    public RemoteProblemDetails? Problem { get; }

    /// <summary>Gets additional response headers.</summary>
    public IReadOnlyDictionary<string, string[]> Headers { get; }
}

/// <summary>Represents an HTTP 400 Bad Request service error.</summary>
public sealed class RemoteBadRequestException : RemoteServiceException
{
    /// <summary>Initializes a new instance of the <see cref="RemoteBadRequestException"/> class.</summary>
    /// <param name="problem">The Problem Details payload.</param>
    public RemoteBadRequestException(RemoteProblemDetails? problem = null) : base(400, problem)
    {
    }
}

/// <summary>Represents an HTTP 401 Unauthorized service error.</summary>
public sealed class RemoteUnauthorizedException : RemoteServiceException
{
    /// <summary>Initializes a new instance of the <see cref="RemoteUnauthorizedException"/> class.</summary>
    /// <param name="problem">The Problem Details payload.</param>
    public RemoteUnauthorizedException(RemoteProblemDetails? problem = null) : base(401, problem)
    {
    }
}

/// <summary>Represents an HTTP 403 Forbidden service error.</summary>
public sealed class RemoteForbiddenException : RemoteServiceException
{
    /// <summary>Initializes a new instance of the <see cref="RemoteForbiddenException"/> class.</summary>
    /// <param name="problem">The Problem Details payload.</param>
    public RemoteForbiddenException(RemoteProblemDetails? problem = null) : base(403, problem)
    {
    }
}

/// <summary>Represents an HTTP 404 Not Found service error.</summary>
public sealed class RemoteNotFoundException : RemoteServiceException
{
    /// <summary>Initializes a new instance of the <see cref="RemoteNotFoundException"/> class.</summary>
    /// <param name="problem">The Problem Details payload.</param>
    public RemoteNotFoundException(RemoteProblemDetails? problem = null) : base(404, problem)
    {
    }
}

/// <summary>Represents an HTTP 409 Conflict service error.</summary>
public sealed class RemoteConflictException : RemoteServiceException
{
    /// <summary>Initializes a new instance of the <see cref="RemoteConflictException"/> class.</summary>
    /// <param name="problem">The Problem Details payload.</param>
    public RemoteConflictException(RemoteProblemDetails? problem = null) : base(409, problem)
    {
    }
}

/// <summary>Represents an HTTP 422 Unprocessable Content service error.</summary>
public sealed class RemoteValidationException : RemoteServiceException
{
    /// <summary>Initializes a new instance of the <see cref="RemoteValidationException"/> class.</summary>
    /// <param name="problem">The Problem Details payload.</param>
    public RemoteValidationException(RemoteProblemDetails? problem = null) : base(422, problem)
    {
    }
}

/// <summary>Represents a non-successful HTTP response received from a remote service.</summary>
public sealed class RemoteCallException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="RemoteCallException"/> class.</summary>
    /// <param name="statusCode">The HTTP response status code.</param>
    /// <param name="operation">The remote operation identifier.</param>
    /// <param name="problem">The parsed Problem Details payload.</param>
    /// <param name="headers">The response headers.</param>
    /// <param name="responseExcerpt">A bounded excerpt from a non-Problem response body.</param>
    public RemoteCallException(
        int statusCode,
        string operation,
        RemoteProblemDetails? problem,
        IReadOnlyDictionary<string, string[]> headers,
        string? responseExcerpt)
        : base(problem?.Detail ?? problem?.Title ?? $"Remote operation '{operation}' failed with HTTP {statusCode}.")
    {
        StatusCode = statusCode;
        Operation = operation;
        Problem = problem;
        Headers = headers;
        ResponseExcerpt = responseExcerpt;
    }

    /// <summary>Gets the HTTP response status code.</summary>
    public int StatusCode { get; }

    /// <summary>Gets the remote operation identifier.</summary>
    public string Operation { get; }

    /// <summary>Gets the parsed Problem Details payload.</summary>
    public RemoteProblemDetails? Problem { get; }

    /// <summary>Gets the response headers.</summary>
    public IReadOnlyDictionary<string, string[]> Headers { get; }

    /// <summary>Gets a bounded excerpt from a non-Problem response body.</summary>
    public string? ResponseExcerpt { get; }
}

/// <summary>Represents a network, protocol, or deserialization failure during a remote call.</summary>
public sealed class RemoteTransportException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="RemoteTransportException"/> class.</summary>
    /// <param name="operation">The remote operation identifier.</param>
    /// <param name="requestUri">The request URI, when available.</param>
    /// <param name="innerException">The underlying transport or serialization exception.</param>
    public RemoteTransportException(string operation, Uri? requestUri, Exception innerException)
        : base($"Remote operation '{operation}' could not be completed.", innerException)
    {
        Operation = operation;
        RequestUri = requestUri;
    }

    /// <summary>Gets the remote operation identifier.</summary>
    public string Operation { get; }

    /// <summary>Gets the request URI, when available.</summary>
    public Uri? RequestUri { get; }
}

#pragma warning restore RCS1194 // Implement exception constructors
