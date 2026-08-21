using System.Text.Json.Serialization;

namespace Ling.RemoteServices.Examples.Shared;

/// <summary>
/// Provides source-generated JSON metadata shared by the server and WebAssembly client.
/// </summary>
[JsonSerializable(typeof(WeatherForecast[]))]
[JsonSerializable(typeof(WeatherForecast))]
public sealed partial class RemoteServiceJsonSerializerContext : JsonSerializerContext
{
}
