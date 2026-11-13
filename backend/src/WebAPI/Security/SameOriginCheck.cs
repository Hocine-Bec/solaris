using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Security;

/// <summary>
/// Cross-site request forgery defence for the session cookie (ADR 7), on top of its SameSite=Strict: a request that
/// changes data and carries the session cookie, or that signs in or out, must come from a page on the API's own
/// address. Browsers send an Origin header with such requests; a request without one (a script, a test, a tool) isn't a
/// browser page on another site, so it passes.
/// </summary>
public sealed class SameOriginCheck(RequestDelegate next, IProblemDetailsService problems)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var guarded = ChangesData(request.Method) &&
                      (request.Cookies.ContainsKey(Session.CookieName) || request.Path.StartsWithSegments("/api/auth"));

        if (guarded && request.Headers.Origin.Count > 0 && !IsSameOrigin(request.Headers.Origin.ToString(), request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await problems.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = "This request must come from the Solaris staff app.",
                    Extensions = { ["code"] = "auth.cross_site_request" }
                }
            });
            return;
        }

        await next(context);
    }

    private static bool ChangesData(string method) =>
        !(HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) ||
          HttpMethods.IsTrace(method));

    // The page's host and port against the API's own: the staff app and the API are served on one site (ADR 7).
    private static bool IsSameOrigin(string origin, HttpRequest request) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
        string.Equals(uri.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase);
}
