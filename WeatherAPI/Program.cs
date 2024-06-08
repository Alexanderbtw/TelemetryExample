using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace WeatherAPI;

internal static class Program
{
    private static readonly ActivitySource _activitySource = new("WeatherAPI", "1.0.0");

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        // builder.Services.AddPrometheusMetrics();
        builder.Services.AddAllTelemetry();
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = "localhost:6379";
            options.InstanceName = AppDomain.CurrentDomain.FriendlyName;
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // app.MapPrometheusScrapingEndpoint();
        app.MapGet("/weatherforecast/{city}", (IDistributedCache cache, string city) =>
            {
                using var activity = _activitySource.StartActivity();

                WeatherForecast? forecast = null;
                var forecastString = cache.GetString(city);
                if (forecastString is not null)
                {
                    activity?.SetTag("fromCache", true);
                    forecast = JsonSerializer.Deserialize<WeatherForecast>(forecastString);
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
                    };

                    forecastString = JsonSerializer.Serialize(forecast);
                    cache.SetString(city, forecastString, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
                    });
                }

                return forecast;
            })
            .WithName("GetWeatherForecast")
            .WithOpenApi();

        var a = AppDomain.CurrentDomain.FriendlyName;
        app.Run();
    }
}

internal static class ServicesExtensions
{
    internal static IServiceCollection AddPrometheusMetrics(this IServiceCollection services)
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

    internal static IServiceCollection AddAllTelemetry(this IServiceCollection services)
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
                .AddRedisInstrumentation()
                .AddOtlpExporter(options =>
                {
                    options.ExportProcessorType = ExportProcessorType.Batch;
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                })
            )
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