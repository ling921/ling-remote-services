using System.Text.Json;
using System.Text.Json.Serialization;
using Ling.RemoteServices.Models;

namespace Ling.RemoteServices.Client;

/// <summary>
/// Provides source-generated JSON metadata for protocol types owned by the client runtime.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(RemoteProblemDetails))]
internal sealed partial class RemoteServiceClientJsonSerializerContext : JsonSerializerContext
{
}
