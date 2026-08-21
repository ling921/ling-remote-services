using System.Text.Json.Serialization;

namespace Ling.RemoteServices.NativeAotSmoke;

/// <summary>
/// Provides source-generated JSON metadata for the Native AOT smoke test.
/// </summary>
[JsonSerializable(typeof(AotEchoRequest))]
[JsonSerializable(typeof(AotEchoResponse))]
public sealed partial class AotJsonSerializerContext : JsonSerializerContext
{
}
