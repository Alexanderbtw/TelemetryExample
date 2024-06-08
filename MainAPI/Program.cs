using MainAPI;
using MainAPI.Services;
using MainAPI.Telemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using ExportProcessorType = OpenTelemetry.ExportProcessorType;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    // builder.Services.AddSerilog();
    builder.Host.AddSerilog();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddWeatherForecast();
    // builder.Services.AddPrometheusMetrics();
    builder.Services.AddAllTelemetry();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // app.MapPrometheusScrapingEndpoint();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Server terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}


internal static class ServicesExtensions
{
    internal static IServiceCollection AddWeatherForecast(this IServiceCollection services)
    {
        services.AddScoped<WeatherService>();
        services.AddHttpClient<WeatherHttpClient>();
        services.AddSingleton<WeatherMetrics>();
        return services;
    }

    internal static IServiceCollection AddPrometheusMetrics(this IServiceCollection services)
    {
        services.AddOpenTelemetry() // OpenTelemetry && OpenTelemetry.Extensions.Hosting
            .ConfigureResource(resourceBuilder => resourceBuilder
                .AddService(WeatherMetrics.ApplicationName, serviceInstanceId: Environment.MachineName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["EnvironmentName"] =
                        WeatherMetrics.GlobalSystemName // That attribute is visible in the target_info separate metric.
                }))
            .WithMetrics(meterProviderBuilder => meterProviderBuilder
                .AddMeter(WeatherMetrics.InstrumentsSourceName)
                .AddAspNetCoreInstrumentation() // OpenTelemetry.Instrumentation.AspNetCore
                .AddHttpClientInstrumentation() // OpenTelemetry.Instrumentation.Http
                .AddRuntimeInstrumentation() // OpenTelemetry.Instrumentation.Runtime
                .AddProcessInstrumentation() // OpenTelemetry.Instrumentation.Process
                .AddPrometheusExporter(opt =>
                {
                    opt.DisableTotalNameSuffixForCounters = false; // Ability to disable total name suffix for counters
                }));
        ; //OpenTelemetry.Exporter.Prometheus.AspNetCore
        return services;
    }

    internal static IServiceCollection AddAllTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry() // OpenTelemetry && OpenTelemetry.Extensions.Hosting
            .ConfigureResource(resourceBuilder => resourceBuilder
                .AddService(WeatherMetrics.ApplicationName, serviceInstanceId: Environment.MachineName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["EnvironmentName"] =
                        WeatherMetrics.GlobalSystemName // That attribute is visible in the target_info separate metric.
                }))
            .WithTracing(tracerProviderBuilder => tracerProviderBuilder
                .AddSource(WeatherMetrics.ApplicationName)
                .SetErrorStatusOnException()
                .SetSampler(new AlwaysOnSampler())
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation(options => { options.RecordException = true; })
                .AddOtlpExporter(options =>
                {
                    options.ExportProcessorType = ExportProcessorType.Batch;
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                })
            )
            .WithMetrics(meterProviderBuilder => meterProviderBuilder
                .AddMeter(WeatherMetrics.InstrumentsSourceName)
                .AddAspNetCoreInstrumentation() // OpenTelemetry.Instrumentation.AspNetCore
                .AddHttpClientInstrumentation() // OpenTelemetry.Instrumentation.Http
                .AddRuntimeInstrumentation() // OpenTelemetry.Instrumentation.Runtime
                .AddProcessInstrumentation() // OpenTelemetry.Instrumentation.Process
                .AddOtlpExporter(options =>
                {
                    options.ExportProcessorType = ExportProcessorType.Batch;
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                })); // OpenTelemetry.Exporter.OpenTelemetryProtocol (default port: 4317)
        return services;
    }

    internal static IHostBuilder AddSerilog(this IHostBuilder host)
    {
        host.UseSerilog((ctx, cfg) =>
        {
            cfg.Enrich.FromLogContext()
                .ReadFrom.Configuration(ctx.Configuration)
                .Enrich.WithProperty("Application", ctx.HostingEnvironment.ApplicationName)
                .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
                .WriteTo.OpenTelemetry(config =>
                {
                    config.IncludedData = IncludedData.TraceIdField | IncludedData.SpanIdField;
                    config.Protocol = OtlpProtocol.Grpc;
                    config.Endpoint = "http://localhost:9095/otlp/v1/logs";
                });
        });
        return host;
    }
}