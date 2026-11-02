namespace Application.DTOs.EnergyProduction;

public class EnergyProductionResponse
{
    public int Id { get; set; }
    public DateOnly ProductionDate { get; set; }
    public decimal ActualProductionKwh { get; set; }
    public decimal ExpectedProductionKwh { get; set; }
    public decimal VarianceKwh { get; set; }
    public decimal VariancePercentage { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? WeatherCondition { get; set; }
}