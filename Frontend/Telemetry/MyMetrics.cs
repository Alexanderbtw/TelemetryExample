using System.Diagnostics.Metrics;
using WeatherAPI;

namespace Frontend.Telemetry;

public class MyMetrics
{
    public static readonly string GlobalSystemName = Environment.MachineName;
    public static readonly string ApplicationName = AppDomain.CurrentDomain.FriendlyName;
    public static readonly string InstrumentsSourceName = nameof(MyMetrics);

    private int _temperature;

    public Counter<int> SummaryRequestsCounter { get; }

    public MyMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory
            .Create(InstrumentsSourceName, "1.0.0");

        SummaryRequestsCounter = meter
            .CreateCounter<int>(name: "city.change.requests",
                unit: "Requests",
                description: "The number of requests to change the city");

        meter.CreateObservableGauge<int>(name: "weather.forecast.temperature",
            observeValue: () => new Measurement<int>(_temperature),
            unit: "Celsius",
            description: "The temperature today");
    }

    public void SetWeather(WeatherForecast forecast)
        => _temperature = forecast.Temperature;
}