using System.Diagnostics;

namespace WebAPI.Middleware;

public class PerformanceMiddleware(RequestDelegate next, ILogger<PerformanceMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path;
        var method = context.Request.Method;
        await next(context);
        stopwatch.Stop();
        logger.LogWarning(
            "⏱️ PERFORMANCE: {Method} {Path} | Total: {ElapsedMs}ms | Status: {StatusCode}",
            method,
            path,
            stopwatch.ElapsedMilliseconds,
            context.Response.StatusCode
        );
    }
}