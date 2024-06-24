using System.Diagnostics;
using WeatherAPI;

namespace Frontend.Services;

public class WeatherService(WeatherHttpClient _weatherHttpClient)
{
    private static readonly ActivitySource _activitySource = new(nameof(WeatherService), "1.0.0");

    public async Task<(bool IsSuccess, WeatherForecast? Data, string? ErrorMessage)> GetForecastAsync(string city)
    {
        using var activity = _activitySource.StartActivity();
        try
        {
            var response = await _weatherHttpClient.GetWeatherAsync(city);
            if (response.IsSuccessStatusCode)
            {
                var weatherData = await response.Content.ReadFromJsonAsync<WeatherForecast>();
                return (true, weatherData, null);
            }

            return response.StatusCode switch
            {
                System.Net.HttpStatusCode.NotFound => (false, null, "City not found."),
                System.Net.HttpStatusCode.InternalServerError => (false, null,
                    "Internal server error. Please try again later."),
                _ => (false, null, $"Unexpected error: {response.StatusCode}")
            };
        }
        catch (HttpRequestException)
        {
            return (false, null, "Network error. Please check your connection and try again.");
        }
        catch (Exception ex)
        {
            return (false, null, $"An unexpected error occurred: {ex.Message}");
        }
    }
}