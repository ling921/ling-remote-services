using Ling.RemoteServices.Attributes;

namespace Ling.RemoteServices.Examples.Shared;

/// <summary>
/// Provides weather forecast data to interactive client components.
/// </summary>
[RemoteService("/api/v1/weather")]
[RemoteCors("RemoteServices")]
[RemoteEndpointPolicy("WeatherEndpoint")]
public interface IWeatherForecastService
{
    /// <summary>Gets weather forecasts.</summary>
    [Get(IsClientDefault = true), Post]
    [RemoteOutputCache("Weather")]
    Task<WeatherForecast[]> GetAsync(CancellationToken cancellationToken = default);
}
