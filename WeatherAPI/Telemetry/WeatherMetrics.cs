using System.Diagnostics.Metrics;

namespace MainAPI.Telemetry;

public class WeatherMetrics
{
    public static readonly string GlobalSystemName = Environment.MachineName;
    public static readonly string ApplicationName = AppDomain.CurrentDomain.FriendlyName;
    public static readonly string InstrumentsSourceName = "WeatherMetrics";

    private WeatherForecast _currentWeather = new();

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
            unit: "Cities",
            description: "The number of unique cities");
    }

    private Measurement<int> GetTemperature() =>
        new(_currentWeather.Temperature, new KeyValuePair<string, object?>("Humidity", _currentWeather.Humidity));

    public void SetWeather(WeatherForecast forecast)
    {
        _currentWeather = forecast;
    }
}