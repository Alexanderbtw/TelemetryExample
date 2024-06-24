namespace WeatherAPI;

public class WeatherForecast
{
    public int Temperature { get; set; }
    public int Humidity { get; set; }
    public int WindSpeed { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    public override string ToString() => "I'm a weather forecast!";
}