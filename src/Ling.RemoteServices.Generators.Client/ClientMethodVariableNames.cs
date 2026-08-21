using Microsoft.CodeAnalysis;

namespace Ling.RemoteServices.Generators;

/// <summary>
/// Allocates generated client local names that cannot collide with contract parameters.
/// </summary>
internal sealed class ClientMethodVariableNames
{
    private readonly HashSet<string> reservedNames;

    public ClientMethodVariableNames(IMethodSymbol method)
    {
        reservedNames = new HashSet<string>(
            method.Parameters.Select(parameter => parameter.Name),
            StringComparer.Ordinal);

        HttpMethod = Create("httpMethod");
        PathValues = Create("pathValues");
        Query = Create("query");
        Uri = Create("uri");
        Request = Create("request");
        Multipart = Create("multipart");
        Response = Create("response");
        Stream = Create("stream");
    }

    public string HttpMethod { get; }

    public string PathValues { get; }

    public string Query { get; }

    public string Uri { get; }

    public string Request { get; }

    public string Multipart { get; }

    public string Response { get; }

    public string Stream { get; }

    public string Create(string purpose)
    {
        var name = "__remoteService" + char.ToUpperInvariant(purpose[0]) + purpose.Substring(1);
        while (!reservedNames.Add(name))
        {
            name += "_";
        }

        return name;
    }
}
