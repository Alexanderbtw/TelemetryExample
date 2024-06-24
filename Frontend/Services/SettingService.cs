using System.Diagnostics;
using Frontend.Models;

namespace Frontend.Services;

public class SettingService()
{
    private static readonly ActivitySource _activitySource = new(nameof(SettingService), "1.0.0");
    private readonly Settings _settings = new Settings();

    public string GetCity()
    {
        using var activity = _activitySource.StartActivity();
        activity?.SetTag("city", _settings.City);
        return _settings.City;
    }

    public void SetCity(string city)
    {
        using var activity = _activitySource.StartActivity();
        activity?.SetTag("city", city);
        _settings.City = city;
    }
}