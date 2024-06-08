using WeatherAPI;

namespace MainAPI;

public class WeatherHttpClient
{
    private readonly HttpClient _httpClient;

    public WeatherHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.BaseAddress = new Uri("http://localhost:9185/");
    }

    public async Task<WeatherForecast?> GetWeatherAsync(string city)
    {
        return await _httpClient.GetFromJsonAsync<WeatherForecast>($"weatherforecast/{city}");
    }
}