namespace Application.DTOs.EnergyProduction;

public class UpdateEnergyProductionRequest
{
    public decimal? ActualProductionKwh { get; set; }
    public decimal? ExpectedProductionKwh { get; set; }
    public string? HealthStatus { get; set; }
    public string? Notes { get; set; }
}