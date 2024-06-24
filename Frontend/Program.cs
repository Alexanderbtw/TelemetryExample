using Frontend;
using Frontend.Controllers;
using Frontend.Services;
using Frontend.Telemetry;
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

    builder.Services.AddControllersWithViews();
    builder.Services.AddWeatherForecast();
    // builder.Services.AddPrometheusMetrics();
    builder.Services.AddAllTelemetry();

    var app = builder.Build();
    app.UseStaticFiles();

    if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");

    app.UseRouting();
    // app.MapPrometheusScrapingEndpoint();
    app.MapControllerRoute("default", "{controller=Home}/{action=Index}");

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
        services.AddSingleton<MyMetrics>();
        services.AddSingleton<SettingService>();
        return services;
    }

    internal static IServiceCollection AddPrometheusMetrics(this IServiceCollection services)
    {
        services.AddOpenTelemetry() // OpenTelemetry && OpenTelemetry.Extensions.Hosting
            .ConfigureResource(resourceBuilder => resourceBuilder
                .AddService(MyMetrics.ApplicationName, serviceInstanceId: Environment.MachineName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["EnvironmentName"] =
                        MyMetrics.GlobalSystemName // That attribute is visible in the target_info separate metric.
                }))
            .WithMetrics(meterProviderBuilder => meterProviderBuilder
                .AddMeter(MyMetrics.InstrumentsSourceName)
                .AddAspNetCoreInstrumentation() // OpenTelemetry.Instrumentation.AspNetCore
                .AddHttpClientInstrumentation() // OpenTelemetry.Instrumentation.Http
                .AddRuntimeInstrumentation() // OpenTelemetry.Instrumentation.Runtime
                .AddProcessInstrumentation() // OpenTelemetry.Instrumentation.Process
                .AddPrometheusExporter(opt =>
                {
                    opt.DisableTotalNameSuffixForCounters = false; // Ability to disable total name suffix for counters
                })); //OpenTelemetry.Exporter.Prometheus.AspNetCore
        return services;
    }

    internal static IServiceCollection AddAllTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry() // OpenTelemetry && OpenTelemetry.Extensions.Hosting
            .ConfigureResource(resourceBuilder => resourceBuilder
                .AddService(MyMetrics.ApplicationName, serviceInstanceId: Environment.MachineName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["EnvironmentName"] =
                        MyMetrics.GlobalSystemName // That attribute is visible in the target_info separate metric.
                }))
            .WithTracing(tracerProviderBuilder => tracerProviderBuilder
                .AddSource(nameof(WeatherForecastController))
                .AddSource(nameof(SettingController))
                .AddSource(nameof(WeatherService))
                .AddSource(nameof(SettingService))
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
                .AddMeter(MyMetrics.InstrumentsSourceName)
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
                .Enrich.WithProperty("EnvironmentName", ctx.HostingEnvironment.EnvironmentName)
                .WriteTo.Console()
                .WriteTo.OpenTelemetry(config =>
                {
                    config.IncludedData = IncludedData.TraceIdField | IncludedData.SpanIdField;
                    config.Protocol = OtlpProtocol.Grpc;
                    config.Endpoint = "http://localhost:4317/otlp/v1/logs";
                    config.ResourceAttributes = new Dictionary<string, object>
                    {
                        { "service.name", ctx.HostingEnvironment.ApplicationName }
                    };
                });
        });
        return host;
    }
}