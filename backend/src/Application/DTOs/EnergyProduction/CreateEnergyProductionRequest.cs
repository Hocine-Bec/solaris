namespace Application.DTOs.EnergyProduction;

public class CreateEnergyProductionRequest
{
    public int InstallationId { get; set; }
    public DateOnly ProductionDate { get; set; }
    public decimal ActualProductionKwh { get; set; }
    public decimal ExpectedProductionKwh { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int? WeatherDataId { get; set; }
}