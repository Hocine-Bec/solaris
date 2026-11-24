using Application.Common.Models;
using Application.DTOs.Production;
using Application.Errors;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Security;

namespace WebAPI.Controllers;

/// <summary>
/// What the switched-on systems produced (FR-P2, FR-P3, FR-P4), against what they should have (BR-P1, BR-P2). The
/// office's screens; a device never reads these.
/// </summary>
[Route("api/production")]
[Authorize(Policy = Policies.Admin)]
public class ProductionController(IProductionService service) : ApiControllerBase
{
    /// <summary>
    /// The fleet's last day that has ended in every zone it stands in (FR-P2): how many systems at each health, and
    /// the 20 doing worst.
    /// </summary>
    [HttpGet("fleet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<FleetDayResponse>> GetFleetDay() => Ok(await service.GetFleetDayAsync());

    /// <summary>One installation's month, day by day (FR-P3), in its own local days.</summary>
    [HttpGet("installations/{id:int}/years/{year:int}/months/{month:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MonthProductionResponse>> GetMonth(int id, int year, int month)
    {
        if (WhyNotAYear(year) is { } badYear)
            return Problem(badYear);
        if (month is < 1 or > 12)
            return Problem(ProductionErrors.NotAMonth);
        var result = await service.GetMonthAsync(id, year, month);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    /// <summary>One installation's year, month by month (FR-P4).</summary>
    [HttpGet("installations/{id:int}/years/{year:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<YearProductionResponse>> GetYear(int id, int year)
    {
        if (WhyNotAYear(year) is { } badYear)
            return Problem(badYear);
        var result = await service.GetYearAsync(id, year);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    private static Error? WhyNotAYear(int year) =>
        year < ProductionErrors.EarliestYear || year > ProductionErrors.LatestYear ? ProductionErrors.NotAYear : null;
}
