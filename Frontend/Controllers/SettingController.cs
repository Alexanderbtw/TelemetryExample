using System.Diagnostics;
using Frontend.Models;
using Frontend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers;

public class SettingController(
    SettingService _settingService,
    ILogger<SettingController> _logger)
    : Controller
{
    [HttpGet]
    public IActionResult Index() => View(new Settings { City = _settingService.GetCity() });

    [HttpPost]
    public IActionResult SetCity(Settings settings)
    {
        using var activity = Activity.Current;
        if (ModelState.IsValid)
        {
            _logger.LogInformation("Setting default city to {City}", settings.City);

            _settingService.SetCity(settings.City);
            return RedirectToAction("Index");
        }

        activity?.SetStatus(ActivityStatusCode.Error, "Invalid model state");
        return View("Index", settings);
    }
}