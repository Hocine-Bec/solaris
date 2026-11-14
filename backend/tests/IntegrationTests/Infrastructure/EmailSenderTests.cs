using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Application.Common.Exceptions;
using Application.Common.Models;
using Application.Interfaces.Services;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Measurement;
using Infrastructure.Services;
using Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using TestUtilities.Base;
using TestUtilities.Factories;
using TestUtilities.Database;

namespace IntegrationTests.Infrastructure;

// The email senders and the measurement harness around them (docs/measurements.md §4), on this test's database.
public class EmailSenderTests(PostgresFixture database) : IntegrationTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly EmailMessage Welcome =
        new("LeadWelcome:123", "jane.doe@example.com", "Jane", "Welcome", "<p>Hello</p>", "Hello");

    #region DeliveryLoggingEmailSender

    [Fact]
    public async Task DeliveryLogging_InnerSenderSucceeds_WritesOneRowAfterIt()
    {
        await using var services = BuildServices(harness: false);
        var sender = new DeliveryLoggingEmailSender(new StubSender(), services.GetRequiredService<IServiceScopeFactory>());
        var before = DateTime.UtcNow;

        await sender.SendAsync(Welcome, Ct);

        var row = await Context.EmailDeliveries.AsNoTracking().SingleAsync(Ct);
        row.NotificationKey.Should().Be("LeadWelcome:123");
        row.Recipient.Should().Be("jane.doe@example.com");
        row.Subject.Should().Be("Welcome");
        row.SentAt.Should().BeOnOrAfter(before.AddMilliseconds(-1)).And.BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task DeliveryLogging_InnerSenderFails_WritesNothingAndRethrows()
    {
        await using var services = BuildServices(harness: false);
        var sender = new DeliveryLoggingEmailSender(new StubSender(fails: true),
            services.GetRequiredService<IServiceScopeFactory>());

        var act = () => sender.SendAsync(Welcome, Ct);

        await act.Should().ThrowAsync<MailServerUnavailableException>();
        (await Context.EmailDeliveries.CountAsync(Ct)).Should().Be(0);
    }

    [Fact]
    public async Task DeliveryLogging_SameNotificationSentTwice_WritesTwoRows()
    {
        // A re-send must show up as a duplicate, so the key is deliberately not unique.
        await using var services = BuildServices(harness: false);
        var sender = new DeliveryLoggingEmailSender(new StubSender(), services.GetRequiredService<IServiceScopeFactory>());

        await sender.SendAsync(Welcome, Ct);
        await sender.SendAsync(Welcome, Ct);

        (await Context.EmailDeliveries.CountAsync(d => d.NotificationKey == "LeadWelcome:123", Ct)).Should().Be(2);
    }

    #endregion

    #region FaultInjectingEmailSender

    [Fact]
    public async Task FaultInjecting_FaultOn_FailsWithoutReachingTheInnerSender()
    {
        var inner = new StubSender();
        var fault = new EmailFaultSwitch(NullLogger<EmailFaultSwitch>.Instance);
        fault.Set(true);

        var act = () => new FaultInjectingEmailSender(inner, fault).SendAsync(Welcome, Ct);

        await act.Should().ThrowAsync<MailServerUnavailableException>();
        inner.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task FaultInjecting_FaultOff_SendsThroughTheInnerSender()
    {
        var inner = new StubSender();
        var fault = new EmailFaultSwitch(NullLogger<EmailFaultSwitch>.Instance);

        await new FaultInjectingEmailSender(inner, fault).SendAsync(Welcome, Ct);

        inner.Sent.Should().ContainSingle().Which.Should().Be(Welcome);
    }

    #endregion

    #region The provider's senders

    [Fact]
    public async Task Recording_WaitsTheSimulatedLatency()
    {
        var sender = new RecordingEmailSender(Options.Create(new EmailOptions { SimulatedLatencyMs = 100 }));
        var stopwatch = Stopwatch.StartNew();

        await sender.SendAsync(Welcome, Ct);

        stopwatch.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(95));
    }

    [Fact]
    public async Task GmailSmtp_MailServerUnreachable_ThrowsMailServerUnavailable()
    {
        // A port that was just free, so nothing listens on it: the connection is refused.
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var options = Options.Create(new EmailOptions
        {
            FromName = "Solaris",
            FromAddress = "sales@example.com",
            Smtp = new SmtpOptions { Host = "127.0.0.1", Port = port }
        });

        var act = () => new GmailSmtpEmailSender(new StubGmailAuth(), options).SendAsync(Welcome, Ct);

        await act.Should().ThrowAsync<MailServerUnavailableException>().WithMessage($"*127.0.0.1:{port}*");
    }

    [Fact]
    public void GmailSmtp_Mime_HasBothPartsTheInlineImageAndTheReplyTo()
    {
        var image = new InlineImage("wordmark@solaris", "image/png", [0x89, 0x50, 0x4e, 0x47]);
        var message = Welcome with
        {
            ReplyTo = new EmailAddress("jane.doe@example.com", "Jane Doe"),
            InlineImages = [image]
        };
        var settings = new EmailOptions { FromName = "Solaris", FromAddress = "office@example.com" };

        var mime = GmailSmtpEmailSender.BuildMime(message, settings);

        mime.From.Mailboxes.Single().Address.Should().Be("office@example.com");
        mime.To.Mailboxes.Single().Address.Should().Be("jane.doe@example.com");
        mime.ReplyTo.Mailboxes.Single().Address.Should().Be("jane.doe@example.com");
        mime.TextBody.Should().Be("Hello");
        mime.HtmlBody.Should().Be("<p>Hello</p>");
        // multipart/alternative: the text, then the HTML with its image (multipart/related)
        var alternative = mime.Body.Should().BeAssignableTo<Multipart>().Subject;
        alternative.ContentType.MimeType.Should().Be("multipart/alternative");
        var related = alternative[1].Should().BeAssignableTo<Multipart>().Subject;
        related.ContentType.MimeType.Should().Be("multipart/related");
        related.OfType<MimePart>().Single(p => p.ContentType.MimeType == "image/png").ContentId
            .Should().Be("wordmark@solaris");
    }

    [Fact]
    public void GmailSmtp_Mime_WithoutReplyTo_HasNone()
    {
        var mime = GmailSmtpEmailSender.BuildMime(Welcome,
            new EmailOptions { FromName = "Solaris", FromAddress = "office@example.com" });

        mime.ReplyTo.Count.Should().Be(0);
    }

    [Theory]
    [InlineData("Recording", typeof(RecordingEmailSender))]
    [InlineData("Gmail", typeof(GmailSmtpEmailSender))]
    public async Task WithoutTheHarness_TheSenderIsTheProviders(string provider, Type expected)
    {
        await using var services = BuildServices(harness: false, provider);
        await using var scope = services.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<IEmailSender>().Should().BeOfType(expected);
    }

    #endregion

    #region The harness, as Development wires it

    [Fact]
    public async Task Harness_TheOutermostSenderLogsEveryAcceptedEmail_AndTheFaultStopsThem()
    {
        await using var services = BuildServices(harness: true);
        await using var scope = services.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        sender.Should().BeOfType<DeliveryLoggingEmailSender>();

        await sender.SendAsync(Welcome, Ct);
        services.GetRequiredService<EmailFaultSwitch>().Set(true);
        var act = () => sender.SendAsync(Welcome with { NotificationKey = "LeadWelcome:124" }, Ct);

        await act.Should().ThrowAsync<MailServerUnavailableException>();
        var keys = await Context.EmailDeliveries.AsNoTracking().Select(d => d.NotificationKey).ToListAsync(Ct);
        keys.Should().Equal("LeadWelcome:123");
    }

    [Fact]
    public async Task Harness_TheHangfireJobs_LogBothNotificationsOfALead()
    {
        // The jobs the current design enqueues for every new lead, each reading the lead by its id: the P2 "before"
        // counts their deliveries.
        await SeedAsync(AddressFactory.CreateFakeAddress(id: 1));
        await SeedAsync(LeadFactory.CreateFakeLead(id: 123, addressId: 1, email: "jane.doe@example.com"));
        await using var services = BuildServices(harness: true);
        await using var scope = services.CreateAsyncScope();
        var jobs = scope.ServiceProvider.GetRequiredService<IEmailQueueHandler>();

        await jobs.SendLeadWelcomeEmailAsync(123);
        await jobs.SendSalesNotificationEmailAsync(123);

        var rows = await Context.EmailDeliveries.AsNoTracking().OrderBy(d => d.Id).ToListAsync(Ct);
        rows.Select(d => (d.NotificationKey, d.Recipient)).Should().Equal(
            ("LeadWelcome:123", "jane.doe@example.com"),
            ("SalesNewLeadAlert:123", "sales@example.com"));
    }

    [Fact]
    public async Task Jobs_LeadNoLongerExists_SendNothingAndDontFail()
    {
        // Retrying can't bring a missing lead back, so the job ends instead of failing into Hangfire's retries
        await using var services = BuildServices(harness: true);
        await using var scope = services.CreateAsyncScope();
        var jobs = scope.ServiceProvider.GetRequiredService<IEmailQueueHandler>();

        await jobs.SendLeadWelcomeEmailAsync(404);
        await jobs.SendSalesNotificationEmailAsync(404);

        (await Context.EmailDeliveries.AnyAsync(Ct)).Should().BeFalse();
    }

    [Theory]
    [InlineData(null, typeof(HangfireLeadNotifier))]
    [InlineData("Hangfire", typeof(HangfireLeadNotifier))]
    [InlineData("Synchronous", typeof(SynchronousLeadNotifier))]
    public async Task Harness_TheLeadNotifier_IsTheConfiguredModes(string? mode, Type expected)
    {
        // Hangfire unless a measurement asks for the P1 "before"
        await using var services = BuildServices(harness: true, notificationsMode: mode);
        await using var scope = services.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<ILeadNotifier>().Should().BeOfType(expected);
    }

    #endregion

    private ServiceProvider BuildServices(bool harness, string provider = "Recording", string? notificationsMode = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = ConnectionString,
            ["Jwt:Secret"] = new string('s', 32),
            ["Jwt:Issuer"] = "solaris-api",
            ["Jwt:Audience"] = "solaris-staff",
            ["Email:Provider"] = provider,
            ["Email:SimulatedLatencyMs"] = "0",
            ["Email:FromName"] = "Solaris",
            ["Email:FromAddress"] = "sales@example.com",
            ["Email:SiteUrl"] = "https://solaris.test",
            ["Email:Smtp:Host"] = "127.0.0.1",
            ["Email:Smtp:Port"] = "25",
            ["Email:Gmail:ClientId"] = "client-id",
            ["Email:Gmail:ClientSecret"] = "client-secret",
            ["Email:Gmail:RefreshToken"] = "refresh-token"
        };
        if (notificationsMode is not null)
            settings["Notifications:Mode"] = notificationsMode;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IHostEnvironment>(new TestHostEnvironment("Development"))
            .AddInfrastructure(configuration);
        if (harness)
            services.AddMeasurementHarness();
        return services.BuildServiceProvider();
    }

    private sealed class StubSender(bool fails = false) : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (fails)
                throw new MailServerUnavailableException("The stub mail server is down.");
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class StubGmailAuth : IGmailAuthService
    {
        public Task<string> GetAccessTokenAsync() => Task.FromResult("access-token");
    }
}
