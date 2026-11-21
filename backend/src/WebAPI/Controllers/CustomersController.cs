using Application.DTOs.Common;
using Application.DTOs.Customers;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Security;

namespace WebAPI.Controllers;

/// <summary>Customers, for the office (FR-C1): the list, searched by name or email, and one with its installations.</summary>
[Route("api/customers")]
[Authorize(Policy = Policies.Admin)]
public class CustomersController(ICustomerService service) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<CustomerSummaryResponse>>> GetPage([FromQuery] CustomerPageRequest request)
    {
        return Ok(await service.GetPageAsync(request));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> GetById(int id)
    {
        var result = await service.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }
}
