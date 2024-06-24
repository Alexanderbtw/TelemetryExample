using System.Diagnostics;
using Frontend.Models;
using Frontend.Services;
using Frontend.Telemetry;
using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers;

public class SettingController(
    SettingService _settingService,
    ILogger<SettingController> _logger,
    MyMetrics _myMetrics)
    : Controller
{
    private static readonly ActivitySource _activitySource = new(nameof(SettingController), "1.0.0");

    [HttpGet]
    public IActionResult Index() => View(new Settings { City = _settingService.GetCity() });

    [HttpPost]
    public IActionResult SetCity(Settings settings)
    {
        using var activity = _activitySource.StartActivity();
        if (ModelState.IsValid)
        {
            _logger.LogInformation("Setting default city to {City}", settings.City);
            _myMetrics.SummaryRequestsCounter.Add(
                delta: 1,
                tag: new KeyValuePair<string, object?>("city", settings.City));

            _settingService.SetCity(settings.City);
            return RedirectToAction("Index");
        }

        activity?.SetStatus(ActivityStatusCode.Error, "Invalid model state");
        return View("Index", settings);
    }
}