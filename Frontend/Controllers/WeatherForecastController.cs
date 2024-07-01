using System.Diagnostics;
using Frontend.Services;
using Frontend.Telemetry;
using Microsoft.AspNetCore.Mvc;
using WeatherAPI;

namespace Frontend.Controllers;

[Route("[controller]")]
public class WeatherForecastController(
    WeatherService _weatherService,
    SettingService _settingService,
    ILogger<WeatherForecastController> _logger,
    MyMetrics myMetrics)
    : Controller
{
    private static readonly ActivitySource _activitySource = new(nameof(WeatherForecastController), "1.0.0");

    [HttpGet]
    public async Task<ActionResult<WeatherForecast>> GetForecast()
    {
        using var activity = _activitySource.StartActivity();
        var city = _settingService.GetCity();

        var (isSuccess, forecast, errorMessage) = await _weatherService.GetForecastAsync(city);
        if (!isSuccess)
        {
            activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
            ViewBag.ErrorMessage = errorMessage!;
            return View("Error");
        }

        activity?.AddEvent(new ActivityEvent($"Weather forecast for {city} is ready"));
        myMetrics.SetWeather(forecast!);

        _logger.LogInformation(
            "Weather forecast equal to {@Forecast}", forecast);

        return View(forecast);
    }
}