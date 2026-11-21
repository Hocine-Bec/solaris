using Application.DTOs.Common;
using Application.DTOs.Lead;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebAPI.Security;

namespace WebAPI.Controllers;

/// <summary>
/// Leads: the office's (Admin), except sending a new one, which the website's quote form does for anyone. A lead's
/// moves (BR-L2), its conversion (BR-L3) and its corrections (FR-L7) each answer the lead as its page shows it.
/// </summary>
[Route("api/leads")]
[Authorize(Policy = Policies.Admin)]
public class LeadsController(ILeadService service) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<LeadSummaryResponse>>> GetPage([FromQuery] LeadPageRequest request)
    {
        return Ok(await service.GetPageAsync(request));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeadResponse>> GetById(int id)
    {
        var result = await service.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    /// <summary>
    /// The quote form: 201 with a new lead's ID, or 200 with the ID of the open lead this email already has (BR-L1),
    /// which gets no new emails. Only the ID, so nobody learns another person's details by typing their email.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableCors(CrossSiteCalls.QuoteForm)]
    [EnableRateLimiting(RateLimits.QuoteForm)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LeadCreatedResponse>> Create([FromBody] CreateLeadRequest request)
    {
        var created = await service.CreateAsync(request);
        var body = new LeadCreatedResponse(created.Id);
        return created.IsNew ? CreatedAtAction(nameof(GetById), new { id = created.Id }, body) : Ok(body);
    }

    [HttpPut("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LeadResponse>> UpdateStatus(int id, [FromBody] UpdateLeadStatusRequest request)
    {
        var result = await service.UpdateStatusAsync(id, request);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    [HttpPost("{id:int}/convert")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LeadResponse>> Convert(int id)
    {
        var result = await service.ConvertAsync(id);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LeadResponse>> Update(int id, [FromBody] UpdateLeadRequest request)
    {
        var result = await service.UpdateAsync(id, request);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }
}
