using Infrastructure.Measurement;
using Infrastructure.Production;

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

        // Adds up every marked day now, rather than waiting for the minutely job, and says how long it took: P4b's
        // "how long the full rollup took" (docs/measurements.md §9)
        app.MapPost("/api/dev/rollups", async (RollupJob job, CancellationToken cancellationToken) =>
            {
                var result = await job.RunAsync(cancellationToken);
                return new RollupRun(result.Batches, result.Days, result.Took.TotalSeconds);
            })
            .WithTags("Measurement (Development only)")
            .AllowAnonymous();

        return app;
    }
}

/// <summary>What a run of the rollup did: <c>{ "batches": 120, "days": 120000, "seconds": 41.2 }</c>.</summary>
public sealed record RollupRun(int Batches, int Days, double Seconds);

/// <summary>Whether every email send fails as if the mail server were unreachable: <c>{ "on": true }</c>.</summary>
public sealed record EmailFaultState(bool On);
