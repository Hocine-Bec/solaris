using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace WebAPI.Security;

/// <summary>The <c>RateLimiting</c> section.</summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>On by default; off only in Development, for the measurement runs, which send many leads from one machine.</summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// How often one client address may call the endpoints anyone can reach and that do costly or sensitive work (BR-S1):
/// the quote form, 5 per minute; and checking a password, 10 per minute, shared by signing in and changing your own
/// password. A fixed window of one minute per address; beyond it, 429 with <c>Retry-After</c>. The counts live in this
/// process, which suits a single instance.
/// </summary>
public static class RateLimits
{
    /// <summary>POST /api/leads</summary>
    public const string QuoteForm = "quote-form";

    /// <summary>POST /api/auth/login and PUT /api/auth/password: one budget for both</summary>
    public const string PasswordChecks = "password-checks";

    public const int QuoteFormPerMinute = 5;
    public const int PasswordChecksPerMinute = 10;

    public static IServiceCollection AddRateLimits(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
            .Validate<IHostEnvironment>((options, environment) => options.Enabled || environment.IsDevelopment(),
                "RateLimiting:Enabled can be false only in Development: it exists for measurements.")
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteTooManyRequestsAsync;
            options.AddPolicy(QuoteForm, context => PerClient(context, QuoteFormPerMinute));
            options.AddPolicy(PasswordChecks, context => PerClient(context, PasswordChecksPerMinute));
        });
        return services;
    }

    /// <summary>Counts the limited endpoints' requests, unless the limits are switched off (Development only).</summary>
    public static IApplicationBuilder UseRateLimits(this WebApplication app)
    {
        if (app.Services.GetRequiredService<IOptions<RateLimitingOptions>>().Value.Enabled)
            app.UseRateLimiter();
        return app;
    }

    private static RateLimitPartition<string> PerClient(HttpContext context, int permitsPerMinute) =>
        RateLimitPartition.GetFixedWindowLimiter(ClientAddress.Of(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitsPerMinute,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });

    // A ProblemDetails like every other error, saying when to try again (a fixed window gives its whole length: by then
    // a new one has started).
    private static async ValueTask WriteTooManyRequestsAsync(OnRejectedContext rejected, CancellationToken _)
    {
        var context = rejected.HttpContext;
        if (rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

        await context.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too Many Requests",
                Detail = "Too many requests from this address. Wait a minute, then try again.",
                Extensions = { ["code"] = "too_many_requests" }
            }
        });
    }
}
