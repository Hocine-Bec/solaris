namespace Application.DTOs.WeatherData;

public class UpdateWeatherDataRequest
{
    public DateOnly? Date { get; set; }
    public string? Condition { get; set; }
    public decimal? TemperatureCelsius { get; set; }
    public decimal? CloudCoverPercentage { get; set; }
}