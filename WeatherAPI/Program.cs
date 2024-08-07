using System.Diagnostics;
using System.Text.Json;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;

namespace WeatherAPI;

internal static class Program
{
    private static readonly IConnectionMultiplexer _redisConnection = ConnectionMultiplexer.Connect("localhost:6379");

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<IConnectionMultiplexer>(_redisConnection);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        // builder.Services.AddPrometheusMetrics();
        builder.Services.AddAllTelemetry();

        var app = builder.Build();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // app.MapPrometheusScrapingEndpoint();
        app.MapGet("/weatherforecast/{city}", (IConnectionMultiplexer connectionMultiplexer, string city) =>
            {
                using var activity = Activity.Current;
                switch (Random.Shared.Next(0, 10))
                {
                    case 0:
                        activity?.SetStatus(ActivityStatusCode.Error, "Getting weather error");
                        return Results.NotFound();
                    case 1:
                        throw new InvalidDataException("Getting weather exception");
                }

                var redis = connectionMultiplexer.GetDatabase();

                WeatherForecast? forecast = null;
                var forecastString = redis.StringGet(city);
                if (forecastString.HasValue)
                {
                    activity?.SetTag("fromCache", true);
                    forecast = JsonSerializer.Deserialize<WeatherForecast>(forecastString.ToString());
                }
                else
                {
                    activity?.SetTag("fromCache", false);

                    Thread.Sleep(1000); // Simulate some work
                    forecast = new WeatherForecast
                    {
                        Temperature = Random.Shared.Next(-20, 55),
                        Humidity = Random.Shared.Next(0, 100),
                        WindSpeed = Random.Shared.Next(0, 20),
                        City = city
                    };
                    forecast.Summary = forecast.Temperature switch
                    {
                        > 30 => "Hot",
                        > 10 => "Normal",
                        _ => "Cold"
                    };

                    forecastString = JsonSerializer.Serialize(forecast);
                    redis.StringSet(city, forecastString, TimeSpan.FromSeconds(10));
                }

                return Results.Json(forecast);
            })
            .WithName("GetWeatherForecast")
            .WithOpenApi();

        app.Run();
    }

    private static IServiceCollection AddPrometheusMetrics(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resourceBuilder => resourceBuilder
                .AddAttributes(new Dictionary<string, object>
                {
                    ["EnvironmentName"] = Environment.MachineName
                }))
            .WithMetrics(meterProviderBuilder => meterProviderBuilder
                .AddAspNetCoreInstrumentation() // Add OpenTelemetry.Instrumentation.AspNetCore nuget package
                .AddHttpClientInstrumentation() // Add OpenTelemetry.Instrumentation.Http nuget package
                .AddRuntimeInstrumentation() // Add OpenTelemetry.Instrumentation.Runtime nuget package
                .AddProcessInstrumentation() // Add OpenTelemetry.Instrumentation.Process nuget package
                .AddPrometheusExporter(opt =>
                {
                    opt.DisableTotalNameSuffixForCounters = true;
                })); // add OpenTelemetry.Exporter.Prometheus.AspNetCore nuget package
        return services;
    }

    private static IServiceCollection AddAllTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resourceBuilder => resourceBuilder
                .AddService(AppDomain.CurrentDomain.FriendlyName, serviceInstanceId: Environment.MachineName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["EnvironmentName"] = Environment.MachineName
                }))
            .WithTracing(tracerProviderBuilder => tracerProviderBuilder
                .AddSource(AppDomain.CurrentDomain.FriendlyName)
                .SetErrorStatusOnException()
                .SetSampler(new AlwaysOnSampler())
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation(options => { options.RecordException = true; })
                .AddRedisInstrumentation(_redisConnection,
                    opt =>
                    {
                        opt.Enrich = (activity, eventName) => activity.SetTag("redis.connection", "localhost:6379");
                        opt.Enrich = (activity, eventName) => activity.SetTag("peer.service", "redis");
                        opt.FlushInterval = TimeSpan.FromSeconds(1);
                        opt.EnrichActivityWithTimingEvents = true;
                    })
                .AddOtlpExporter(options =>
                {
                    options.ExportProcessorType = ExportProcessorType.Batch;
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                }))
            .WithMetrics(meterProviderBuilder => meterProviderBuilder
                .AddAspNetCoreInstrumentation() // Add OpenTelemetry.Instrumentation.AspNetCore nuget package
                .AddHttpClientInstrumentation() // Add OpenTelemetry.Instrumentation.Http nuget package
                .AddRuntimeInstrumentation() // Add OpenTelemetry.Instrumentation.Runtime nuget package
                .AddProcessInstrumentation() // Add OpenTelemetry.Instrumentation.Process nuget package
                .AddOtlpExporter(options =>
                {
                    options.ExportProcessorType = ExportProcessorType.Batch;
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                })); // OpenTelemetry.Exporter.OpenTelemetryProtocol (default port: 4317)
        return services;
    }
}