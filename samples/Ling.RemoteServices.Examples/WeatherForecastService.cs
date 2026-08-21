using Ling.RemoteServices.Examples.Shared;

namespace Ling.RemoteServices.Examples;

public sealed class WeatherForecastService : IWeatherForecastService
{
    public Task<WeatherForecast[]> GetAsync(CancellationToken cancellationToken = default)
    {
        var startDate = DateOnly.FromDateTime(DateTime.Now);
        string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];
        return Task.FromResult(Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = startDate.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = summaries[Random.Shared.Next(summaries.Length)]
        }).ToArray());
    }
}
