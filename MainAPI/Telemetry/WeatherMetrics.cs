using System.Diagnostics.Metrics;
using WeatherAPI;

namespace MainAPI.Telemetry;

public class WeatherMetrics
{
    public static readonly string GlobalSystemName = Environment.MachineName;
    public static readonly string ApplicationName = AppDomain.CurrentDomain.FriendlyName;
    public static readonly string InstrumentsSourceName = "WeatherMetrics";

    private int _temperature;
    private readonly object _lock = new();

    public Counter<int> SummaryRequestsCounter { get; }

    public WeatherMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory
            .Create(InstrumentsSourceName, "1.0.0");

        SummaryRequestsCounter = meter
            .CreateCounter<int>(name: "weather.summary.requests",
                unit: "Requests",
                description: "The number of requests to get a weather summary");

        meter.CreateObservableGauge<int>(name: "weather.forecast.temperature",
            observeValue: () => GetTemperature(),
            unit: "Celsius",
            description: "The temperature today");
    }

    private Measurement<int> GetTemperature() =>
        new(_temperature);

    public void SetWeather(WeatherForecast forecast)
    {
        lock (_lock)
        {
            _temperature = forecast.Temperature;
        }
    }
}