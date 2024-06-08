using System.Diagnostics;
using WeatherAPI;

namespace MainAPI.Services;

public class WeatherService(WeatherHttpClient _weatherHttpClient)
{
    private static readonly ActivitySource _activitySource = new("MainAPI.WeatherService", "1.0.0");

    public async Task<WeatherForecast> GetForecastAsync(string city)
    {
        using var activity = _activitySource.StartActivity();
        return await _weatherHttpClient.GetWeatherAsync(city);
    }

    public async Task<string> GetSummaryAsync(string city)
    {
        using var activity = _activitySource.StartActivity();
        var forecast = await _weatherHttpClient.GetWeatherAsync(city);
        return forecast.Temperature switch
        {
            > 30 => "Hot",
            > 10 => "Normal",
            _ => "Cold"
        };
    }
}