namespace Application.DTOs.EnergyProduction;

public class MonthlyProductionResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TotalProductionKwh { get; set; }
    public decimal AverageDailyKwh { get; set; }
    public int DaysRecorded { get; set; }
}