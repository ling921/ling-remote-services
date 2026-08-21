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
    /// <summary>Searches weather forecasts.</summary>
    [Get("forecasts", IsClientDefault = true), Post("forecasts/search")]
    [RemoteOutputCache("Weather")]
    Task<WeatherForecast[]> SearchAsync(
        [Query] WeatherForecastQuery query,
        [Header("X-Correlation-ID")] string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a weather forecast for a specific date.</summary>
    [Get("forecasts/{date}")]
    Task<WeatherForecast> GetAsync(
        [Path] DateOnly date,
        CancellationToken cancellationToken = default);
}
