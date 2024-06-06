namespace MainAPI;

public class WeatherForecast
{
    public int Temperature { get; set; }
    public int Humidity { get; set; }
    public int WindSpeed { get; set; }

    public override string ToString() => "I'm a weather forecast!";
}