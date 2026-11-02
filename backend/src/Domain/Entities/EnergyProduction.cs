using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Daily energy production records
/// </summary>
public class EnergyProduction
{
    public int Id { get; set; }
    public DateOnly ProductionDate { get; set; }
    public decimal ActualProductionKwh { get; set; }
    public decimal ExpectedProductionKwh { get; set; }
    public SystemHealthStatus HealthStatus { get; set; }
    public string? Notes { get; set; }
        
    public int InstallationId { get; set; }
    public required Installation Installation { get; set; }
        
    // Optional: Weather reference
    public int? WeatherDataId { get; set; }
    public WeatherData? WeatherData { get; set; }
}