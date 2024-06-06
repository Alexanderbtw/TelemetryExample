using System.Diagnostics;
using MainAPI.Services;
using MainAPI.Telemetry;
using Microsoft.AspNetCore.Mvc;

namespace MainAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController(
    WeatherService _weatherService,
    ILogger<WeatherForecastController> _logger,
    WeatherMetrics _weatherMetrics)
    : ControllerBase
{
    private static ActivitySource _activitySource = new("MainAPI.WeatherForecast", "1.0.0");

    [HttpGet("Summary")]
    public ActionResult<string> GetSummary(string city)
    {
        var activity = _activitySource.StartActivity(nameof(GetSummary));
        _logger.LogInformation("Getting weather forecast for {city}.", city);
        _weatherMetrics.SummaryRequestsCounter.Add(
            delta: 1,
            tag: new KeyValuePair<string, object?>("city", city));

        switch (Random.Shared.Next(0, 10))
        {
            case 0:
                activity?.SetStatus(ActivityStatusCode.Error, "Summary error");
                return NotFound();
            case 1:
                throw new InvalidDataException("Summary exception");
        }

        return _weatherService.GetSummary(city);
    }

    [HttpGet]
    public ActionResult<WeatherForecast> GetForecast(string city)
    {
        var activity = _activitySource.StartActivity(nameof(GetSummary));

        var forecast = _weatherService.GetForecast(city);

        activity?.AddEvent(new ActivityEvent($"Weather forecast for {city} is ready"));
        activity?.SetTag("windSpeed", forecast.WindSpeed);
        _weatherMetrics.SetWeather(forecast);

        _logger.LogInformation(
            "Weather forecast for {city} equal to {@forecast}.",
            forecast, city);

        return forecast;
    }
}