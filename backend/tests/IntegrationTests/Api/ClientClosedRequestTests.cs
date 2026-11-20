using FluentAssertions;
using Microsoft.AspNetCore.Http;
using WebAPI.Startup;

namespace IntegrationTests.Api;

// A request its client abandoned ends as 499, not as an exception the log reports as a 500; any other cancellation
// still fails. The middleware alone, with no API and no database.
public class ClientClosedRequestTests
{
    private static async Task<(Func<Task> Run, HttpContext Context)> RequestAsync(bool clientLeft, Exception thrown,
        bool responseStarted = false)
    {
        var context = new DefaultHttpContext();
        using var left = new CancellationTokenSource();
        if (clientLeft) await left.CancelAsync();
        context.RequestAborted = left.Token;
        if (responseStarted)
            context.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(new StartedResponse());
        var middleware = new ClientClosedRequest(_ => throw thrown);
        return (() => middleware.InvokeAsync(context), context);
    }

    [Theory]
    [InlineData(typeof(OperationCanceledException))]
    [InlineData(typeof(TaskCanceledException))]
    [InlineData(typeof(IOException))]
    public async Task ClientLeft_TheRequestEndsAs499_WithoutAnException(Type thrown)
    {
        var (run, context) = await RequestAsync(clientLeft: true, (Exception)Activator.CreateInstance(thrown)!);

        await run.Should().NotThrowAsync();

        context.Response.StatusCode.Should().Be(StatusCodes.Status499ClientClosedRequest);
    }

    [Fact]
    public async Task CancelledWhileTheClientWaits_StaysAnError()
    {
        // A timeout inside the API, say: the client is still there and gets the 500
        var (run, _) = await RequestAsync(clientLeft: false, new OperationCanceledException());

        await run.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ClientLeft_OtherException_StaysAnError()
    {
        var (run, _) = await RequestAsync(clientLeft: true, new InvalidOperationException("A bug."));

        await run.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ClientLeftAfterTheAnswerStarted_TheStatusStays()
    {
        var (run, context) = await RequestAsync(clientLeft: true, new IOException(), responseStarted: true);

        await run.Should().NotThrowAsync();

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    private sealed class StartedResponse : Microsoft.AspNetCore.Http.Features.HttpResponseFeature
    {
        public override bool HasStarted => true;
    }
}
