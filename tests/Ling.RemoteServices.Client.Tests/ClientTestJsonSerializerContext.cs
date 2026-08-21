using System.Text.Json.Serialization;

namespace Ling.RemoteServices.Client.Tests;

/// <summary>
/// Provides source-generated JSON metadata for client runtime tests.
/// </summary>
[JsonSerializable(typeof(ClientTestPayload))]
public sealed partial class ClientTestJsonSerializerContext : JsonSerializerContext
{
}
