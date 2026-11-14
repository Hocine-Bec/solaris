using Infrastructure.Measurement;

namespace WebAPI.Measurement;

/// <summary>
/// The measurement harness's endpoints (docs/measurements.md §4), mapped in Development only and open to anyone there,
/// so the measurement scripts don't sign in.
/// </summary>
public static class MeasurementEndpoints
{
    public static IEndpointRouteBuilder MapMeasurementEndpoints(this IEndpointRouteBuilder app)
    {
        var emailFault = app.MapGroup("/api/dev/email-fault").WithTags("Measurement (Development only)")
            .AllowAnonymous();

        emailFault.MapGet("", (EmailFaultSwitch fault) => new EmailFaultState(fault.IsOn));

        emailFault.MapPut("", (EmailFaultState request, EmailFaultSwitch fault) =>
        {
            fault.Set(request.On);
            return Results.NoContent();
        });

        return app;
    }
}

/// <summary>Whether every email send fails as if the mail server were unreachable: <c>{ "on": true }</c>.</summary>
public sealed record EmailFaultState(bool On);
