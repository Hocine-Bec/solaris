using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Application.Common.Security;
using Application.Errors;
using Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Auth;

/// <summary>How a device proves which installation it is (BR-T2, ADR 14): its key, in <c>X-Device-Key</c>.</summary>
public static class DeviceKeyAuthentication
{
    public const string Scheme = "DeviceKey";
    public const string Header = "X-Device-Key";

    /// <summary>The installation the key belongs to: the only claim a device has.</summary>
    public const string InstallationIdClaim = "installation_id";
}

/// <summary>
/// Reads <c>X-Device-Key</c>: one indexed read by the key's prefix, for an Active installation, then its secret compared
/// with the stored hash in constant time. Nothing is cached, so a key replaced or deleted stops working at the next
/// request. Only the endpoints whose policy names this scheme run it; the staff endpoints never do. Every refusal answers
/// the same <c>401 device.invalid_key</c>.
/// </summary>
public sealed class DeviceKeyHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IInstallationRepo installations,
    IProblemDetailsService problems) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(DeviceKeyAuthentication.Header, out var sent))
            return AuthenticateResult.NoResult();
        if (sent.Count != 1 || !DeviceKeys.TryParse(sent[0], out var prefix, out var secret))
            return AuthenticateResult.Fail("The device key isn't written as one.");
        if (await installations.FindActiveDeviceKeyAsync(prefix) is not { } key || !DeviceKeys.Matches(secret, key.Hash))
            return AuthenticateResult.Fail("No Active installation has this device key.");

        var identity = new ClaimsIdentity(
            [new Claim(DeviceKeyAuthentication.InstallationIdClaim, key.Id.ToString(CultureInfo.InvariantCulture))],
            DeviceKeyAuthentication.Scheme);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), DeviceKeyAuthentication.Scheme));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        await problems.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = Context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Detail = DeviceErrors.InvalidKey.Message,
                Extensions = { ["code"] = DeviceErrors.InvalidKey.Code }
            }
        });
    }
}
