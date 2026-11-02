namespace Application.DTOs.EnergyProduction;

public class EnergyProductionChartResponse
{
    public DateOnly Date { get; set; }
    public decimal ActualKwh { get; set; }
    public decimal ExpectedKwh { get; set; }
    public string? WeatherCondition { get; set; }
}