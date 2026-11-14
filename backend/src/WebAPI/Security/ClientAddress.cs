using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using IPNetwork = System.Net.IPNetwork;
using Microsoft.Extensions.Options;

namespace WebAPI.Security;

/// <summary>
/// The <c>ForwardedHeaders</c> section: the proxies whose <c>X-Forwarded-For</c> and <c>X-Forwarded-Proto</c> the API
/// believes, as networks (<c>10.0.0.0/8</c>, or <c>10.1.2.3/32</c> for one address). Empty by default: no header is
/// believed, so nobody can claim another address to get past the rate limits.
/// </summary>
public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    public string[] KnownNetworks { get; set; } = [];
}

/// <summary>
/// Who a request comes from, for the rate limits: the connection's address, or, behind a proxy the settings name, the
/// address that proxy says it forwarded.
/// </summary>
public static class ClientAddress
{
    public static IServiceCollection AddClientAddress(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ForwardedHeadersSettings>()
            .Bind(configuration.GetSection(ForwardedHeadersSettings.SectionName))
            .Validate(settings => settings.KnownNetworks.All(n => IPNetwork.TryParse(n, out _)),
                "ForwardedHeaders:KnownNetworks must list networks such as 10.0.0.0/8, or 10.1.2.3/32 for one proxy.")
            .ValidateOnStart();

        services.AddOptions<ForwardedHeadersOptions>()
            .Configure<IOptions<ForwardedHeadersSettings>>((options, settings) =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                // Only the listed networks: the framework's defaults (this machine) go too
                options.KnownProxies.Clear();
                options.KnownIPNetworks.Clear();
                foreach (var network in settings.Value.KnownNetworks)
                    options.KnownIPNetworks.Add(IPNetwork.Parse(network));
                // The last address in the header, the one the trusted proxy added: earlier ones came from the client
                options.ForwardLimit = 1;
            });
        return services;
    }

    /// <summary>
    /// Believes the listed proxies' forwarded headers. With none listed, the middleware isn't added at all: with empty
    /// lists it would believe every sender.
    /// </summary>
    public static IApplicationBuilder UseClientAddress(this WebApplication app)
    {
        if (app.Services.GetRequiredService<IOptions<ForwardedHeadersSettings>>().Value.KnownNetworks.Length > 0)
            app.UseForwardedHeaders();
        return app;
    }

    /// <summary>The request's client address, as the rate limits count it; "unknown" when there is none.</summary>
    public static string Of(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;
        if (address is null)
            return "unknown";
        // An IPv4 client on a dual-stack socket arrives as ::ffff:a.b.c.d: the same client either way
        return (address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address).ToString();
    }
}
