using System.Diagnostics;
using MainAPI.Repositories;

namespace MainAPI.Services;

public class WeatherService(WeatherRepository weatherRepository)
{
    private static ActivitySource _activitySource = new("MainAPI.WeatherService", "1.0.0");
    private readonly WeatherRepository _weatherRepository = weatherRepository;

    public WeatherForecast GetForecast(string city)
    {
        using var activity = _activitySource.StartActivity(nameof(GetForecast));
        return _weatherRepository.GetForecast(city);
    }

    public string GetSummary(string city)
    {
        using var activity = _activitySource.StartActivity();
        var forecast = _weatherRepository.GetForecast(city);
        return forecast.Temperature switch
        {
            > 30 => "Hot",
            > 10 => "Normal",
            _ => "Cold"
        };
    }
}