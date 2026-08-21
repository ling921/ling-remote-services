namespace Ling.RemoteServices.Examples.Shared;

/// <summary>
/// Filters weather forecasts through query-string parameters.
/// </summary>
public sealed class WeatherForecastQuery
{
    /// <summary>The first forecast date.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>The number of days to return.</summary>
    public int Days { get; set; } = 5;

    /// <summary>An optional minimum Celsius temperature.</summary>
    public int? MinimumTemperatureC { get; set; }
}

/// <summary>
/// A simple model for a weather forecast.
/// </summary>
public class WeatherForecast
{
    /// <summary>
    /// The date of the weather forecast.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// The temperature in Celsius.
    /// </summary>
    public int TemperatureC { get; set; }

    /// <summary>
    /// The summary of the weather forecast.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// The temperature in Fahrenheit.
    /// </summary>
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
