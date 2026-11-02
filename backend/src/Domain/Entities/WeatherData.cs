namespace Domain.Entities;

/// <summary>
/// Weather conditions for energy production correlation
/// </summary>
public class WeatherData
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Location { get; set; } = string.Empty; // City or coordinates
    public string Condition { get; set; } = string.Empty; // "Sunny", "Cloudy", "Rainy"
    public decimal TemperatureCelsius { get; set; }
    public decimal? CloudCoverPercentage { get; set; }
}