namespace Application.DTOs.EnergyProduction;

public class EnergyProductionSummaryResponse
{
    public decimal TotalProductionKwh { get; set; }
    public decimal AverageDailyProductionKwh { get; set; }
    public decimal BestDayProductionKwh { get; set; }
    public DateOnly? BestProductionDate { get; set; }
    public decimal TotalExpectedKwh { get; set; }
    public decimal OverallVariancePercentage { get; set; }
    public int DaysTracked { get; set; }
    public List<EnergyProductionResponse> RecentProduction { get; set; } = new();
}