using Ling.RemoteServices.Examples.Shared;
using Ling.RemoteServices.Exceptions;
using Ling.RemoteServices.Models;

namespace Ling.RemoteServices.Examples;

public sealed class WeatherForecastService : IWeatherForecastService
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild",
        "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public Task<WeatherForecast[]> SearchAsync(
        WeatherForecastQuery query,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        _ = correlationId;

        if (query.Days is < 1 or > 14)
        {
            throw new RemoteBadRequestException(new RemoteProblemDetails
            {
                Title = "Invalid forecast range",
                Detail = "Days must be between 1 and 14."
            });
        }

        var forecasts = Enumerable.Range(0, query.Days)
            .Select(index => CreateForecast(query.StartDate.AddDays(index)))
            .Where(forecast => query.MinimumTemperatureC is null
                || forecast.TemperatureC >= query.MinimumTemperatureC)
            .ToArray();

        return Task.FromResult(forecasts);
    }

    public Task<WeatherForecast> GetAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (date < today || date > today.AddDays(13))
        {
            throw new RemoteNotFoundException(new RemoteProblemDetails
            {
                Title = "Forecast not found",
                Detail = $"No forecast is available for {date:yyyy-MM-dd}."
            });
        }

        return Task.FromResult(CreateForecast(date));
    }

    private static WeatherForecast CreateForecast(DateOnly date)
    {
        var seed = date.DayNumber;
        var temperatureC = seed % 75 - 20;
        return new WeatherForecast
        {
            Date = date,
            TemperatureC = temperatureC,
            Summary = Summaries[Math.Abs(seed) % Summaries.Length]
        };
    }
}
