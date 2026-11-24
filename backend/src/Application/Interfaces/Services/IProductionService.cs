using Application.Common.Models;
using Application.DTOs.Production;

namespace Application.Interfaces.Services;

/// <summary>What the switched-on systems produced (FR-P2, FR-P3, FR-P4), against what they should have (BR-P1, BR-P2).</summary>
public interface IProductionService
{
    /// <summary>The fleet's last day that has ended everywhere it stands (FR-P2), with the 20 doing worst.</summary>
    Task<FleetDayResponse> GetFleetDayAsync();

    /// <summary>One installation's month, day by day (FR-P3).</summary>
    Task<Result<MonthProductionResponse>> GetMonthAsync(int installationId, int year, int month);

    /// <summary>One installation's year, month by month (FR-P4).</summary>
    Task<Result<YearProductionResponse>> GetYearAsync(int installationId, int year);
}
