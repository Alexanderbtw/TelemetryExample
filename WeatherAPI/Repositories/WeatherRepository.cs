using System.Diagnostics;

namespace MainAPI.Repositories;

public class WeatherRepository
{
    private static ActivitySource _activitySource = new("MainAPI.WeatherRepository", "1.0.0");

    public WeatherForecast GetForecast(string city)
    {
        using var activity = _activitySource.StartActivity();
        return new WeatherForecast
        {
            Temperature = Random.Shared.Next(-20, 55),
            Humidity = Random.Shared.Next(0, 100),
            WindSpeed = Random.Shared.Next(0, 20),
        };
    }
}