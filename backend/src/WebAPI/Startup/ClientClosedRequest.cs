namespace WebAPI.Startup;

/// <summary>
/// A request its client abandoned (a screen left before its data came, a closed tab): the API stops its work, as the
/// request's token asks, and the request ends as <c>499 Client Closed Request</c>, logged as an ordinary request rather
/// than as an error. A cancellation the client didn't cause, such as a timeout inside the API, is still an error.
/// </summary>
public sealed class ClientClosedRequest(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException &&
                                   context.RequestAborted.IsCancellationRequested)
        {
            // Nobody reads the answer; the status is for the log
            if (!context.Response.HasStarted)
                context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }
    }
}
