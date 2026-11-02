namespace Application.DTOs.WeatherData;

public class CreateWeatherDataRequest
{
    public DateOnly Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public decimal TemperatureCelsius { get; set; }
    public decimal? CloudCoverPercentage { get; set; }
}