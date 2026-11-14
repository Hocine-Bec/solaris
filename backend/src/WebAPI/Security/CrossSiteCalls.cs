using Microsoft.Extensions.Options;

namespace WebAPI.Security;

/// <summary>
/// The <c>Cors</c> section: the sites whose pages may send the quote form from a browser, as origins
/// (<c>https://www.example.com</c>). Empty by default: no other site may. The staff app needs none, as it calls the API
/// on its own site (ADR 7).
/// </summary>
public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [];
}

/// <summary>
/// Which other sites' pages may call the API from a browser (BR-S2): only the quote form, from the origins the
/// settings list, without cookies. Every other endpoint answers no other site.
/// </summary>
public static class CrossSiteCalls
{
    /// <summary>The marketing site's quote form: POST /api/leads</summary>
    public const string QuoteForm = "quote-form";

    public static IServiceCollection AddCrossSiteCalls(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CorsSettings>()
            .Bind(configuration.GetSection(CorsSettings.SectionName))
            .Validate(settings => settings.AllowedOrigins.All(IsOrigin),
                "Cors:AllowedOrigins must list origins such as https://www.example.com: http or https, a host, an " +
                "optional port, and no path.")
            .ValidateOnStart();

        services.AddCors();
        services.AddOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>()
            .Configure<IOptions<CorsSettings>>((options, settings) =>
                options.AddPolicy(QuoteForm, policy => policy
                    .WithOrigins(settings.Value.AllowedOrigins)
                    .WithMethods(HttpMethods.Post)
                    // The form sends JSON; Accept needs no permission
                    .WithHeaders("Content-Type")));
        return services;
    }

    // The exact form a browser sends in its Origin header: no trailing slash, path, query or fragment, and no wildcard.
    private static bool IsOrigin(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) &&
        string.Equals(uri.GetLeftPart(UriPartial.Authority), value, StringComparison.OrdinalIgnoreCase);
}
