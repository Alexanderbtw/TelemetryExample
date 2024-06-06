using MainAPI.Repositories;
using MainAPI.Services;
using MainAPI.Telemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

// Log.Logger = new LoggerConfiguration()
//     .Enrich.FromLogContext()
//     .WriteTo.Console()
//     .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
// builder.Services.AddSerilog();
// builder.Host.UseSerilog((ctx, cfg) =>
// {
//     cfg.Enrich.FromLogContext()
//         .Enrich.WithProperty("Application", ctx.HostingEnvironment.ApplicationName)
//         .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
//         .WriteTo.OpenTelemetry(options =>
//         {
//             options.Protocol = OtlpProtocol.Grpc;
//             options.Endpoint = "http://localhost:9095/otlp/v1/logs";
//         });
// });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddWeatherForecast();
builder.Services.AddPrometheusMetrics();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPrometheusScrapingEndpoint();
app.MapControllers();

app.Run();


internal static class ServicesExtensions
{
    internal static IServiceCollection AddWeatherForecast(this IServiceCollection services)
    {
        services.AddScoped<WeatherService>();
        services.AddScoped<WeatherRepository>();
        services.AddSingleton<WeatherMetrics>();
        return services;
    }

    internal static IServiceCollection AddPrometheusMetrics(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resourceBuilder => resourceBuilder
                .AddService(WeatherMetrics.ApplicationName, serviceInstanceId: Environment.MachineName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["EnvironmentName"] =
                        WeatherMetrics.GlobalSystemName // That attribute is visible in the target_info separate metric.
                }))
            .WithMetrics(meterProviderBuilder => meterProviderBuilder
                .AddMeter(WeatherMetrics.InstrumentsSourceName)
                .AddAspNetCoreInstrumentation() // Add OpenTelemetry.Instrumentation.AspNetCore nuget package
                .AddHttpClientInstrumentation() // Add OpenTelemetry.Instrumentation.Http nuget package
                .AddRuntimeInstrumentation() // Add OpenTelemetry.Instrumentation.Runtime nuget package
                .AddPrometheusExporter()); // add OpenTelemetry.Exporter.Prometheus.AspNetCore nuget package
        // .AddProcessInstrumentation() // Add OpenTelemetry.Instrumentation.Process nuget package
        return services;
    }
}