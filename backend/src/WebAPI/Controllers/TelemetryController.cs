using System.Globalization;
using Application.DTOs.Telemetry;
using Application.Errors;
using Application.Interfaces.Services;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Security;

namespace WebAPI.Controllers;

/// <summary>
/// What the installed systems send (FR-T1, FR-T2). The only endpoint a device can reach, and the only one a staff
/// session can't: it is authenticated by the installation's device key alone (BR-T2, ADR 14).
/// </summary>
[Route("api/telemetry")]
[Authorize(AuthenticationSchemes = DeviceKeyAuthentication.Scheme, Policy = Policies.Device)]
public class TelemetryController(ITelemetryService service) : ApiControllerBase
{
    /// <summary>
    /// A batch of up to 500 readings for the installation the key names. The installation is never taken from the
    /// body, so a device can only ever write for itself.
    /// </summary>
    [HttpPost("readings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UploadReadingsResponse>> UploadReadings([FromBody] UploadReadingsRequest request)
    {
        if (CurrentInstallationId is not { } installationId)
            return Problem(DeviceErrors.InvalidKey);
        var result = await service.UploadAsync(installationId, request);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    /// <summary>The installation the device key belongs to; null if the request came without one.</summary>
    private int? CurrentInstallationId =>
        int.TryParse(User.FindFirst(DeviceKeyAuthentication.InstallationIdClaim)?.Value, NumberStyles.None,
            CultureInfo.InvariantCulture, out var id)
            ? id
            : null;
}
