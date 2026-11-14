using Application.Common.Models;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Services;
using Infrastructure.Services.Email;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace IntegrationTests.Infrastructure;

// A new lead's two emails, rendered from a lead, with no database: what each says, and that every value from the form
// is encoded before it goes into the HTML, so a name, an address or a note can't add a link or an image to an email
// Solaris sends.
public class LeadEmailsTests
{
    private const string SiteUrl = "https://solaris.test/";
    private const string Link = "<a href=\"https://evil.example\">Verify your account</a>";

    private static Lead Maya(Action<Lead>? change = null)
    {
        var lead = new Lead
        {
            Id = 128, FirstName = "Maya", LastName = "Brooks", Email = "maya.brooks@example.com",
            PhoneNumber = "(512) 555-0147", PropertyType = PropertyType.House, IsPropertyOwner = true,
            MonthlyBillRange = "100-200", BestTimeToContact = "Morning",
            Notes = "Two big oaks shade the south side.\nCall after 9.",
            CreatedAt = new DateTime(2026, 11, 13, 20, 58, 0, DateTimeKind.Utc),
            Address = new Address { Street = "2104 Oak Hollow Dr", City = "Austin", State = "TX", ZipCode = "78704", Country = "US" }
        };
        change?.Invoke(lead);
        return lead;
    }

    #region The welcome email

    [Fact]
    public void Welcome_SaysWhatHappensNextAndWhatTheySent()
    {
        var email = LeadEmails.Welcome(Maya(), SiteUrl);

        email.Subject.Should().Be("We have your quote request");
        email.Preheader.Should().Be("We'll call you in the morning (8 am to 12 pm) about 2104 Oak Hollow Dr.");
        email.Html.Should().Contain("Thanks for asking, Maya.")
            .And.Contain("We call you on (512) 555-0147 in the morning (8 am to 12 pm).")
            .And.Contain("2104 Oak Hollow Dr, Austin, TX 78704")
            .And.Contain("House, you own it")
            .And.Contain("$100 to $200")
            .And.Contain("href=\"https://solaris.test/how-it-works\"")
            .And.Contain($"src=\"cid:{Wordmark.ContentId}\"")
            .And.Contain("Solaris Energy is fictional");
        email.Text.Should().Contain("01  We call you on (512) 555-0147 in the morning (8 am to 12 pm).")
            .And.Contain("Property:      2104 Oak Hollow Dr, Austin, TX 78704")
            .And.Contain("See what happens at each one: https://solaris.test/how-it-works")
            .And.Contain("Solaris Energy is fictional");
    }

    [Fact]
    public void Welcome_NoteOnSeveralLines_KeepsItsLineBreaks()
    {
        var email = LeadEmails.Welcome(Maya(), SiteUrl);

        email.Html.Should().Contain("Two big oaks shade the south side.<br>Call after 9.");
        email.Text.Should().Contain("Your note:     Two big oaks shade the south side.\n               Call after 9.");
    }

    [Fact]
    public void Welcome_NoNote_LeavesTheNoteOut()
    {
        var email = LeadEmails.Welcome(Maya(l => l.Notes = null), SiteUrl);

        email.Html.Should().NotContain("Your note");
        email.Text.Should().NotContain("Your note");
    }

    [Fact]
    public void Welcome_BusinessThatLeases_SaysSoInTheSiteWords()
    {
        var email = LeadEmails.Welcome(Maya(l =>
        {
            l.PropertyType = PropertyType.Commercial;
            l.IsPropertyOwner = false;
            l.MonthlyBillRange = "300plus";
            l.BestTimeToContact = "Evening";
        }), SiteUrl);

        email.Html.Should().Contain("Business premises, you rent or lease it")
            .And.Contain("Over $300")
            .And.Contain("in the evening (5 to 8 pm)");
    }

    #endregion

    #region The office's alert

    [Fact]
    public void NewLeadAlert_SaysWhoWhereAndWhenToCall()
    {
        var email = LeadEmails.NewLeadAlert(Maya());

        email.Subject.Should().Be("New lead: Maya Brooks, Austin, TX");
        email.Preheader.Should().Be("House, owner, bill $100 to $200. Call in the morning (8 am to 12 pm).");
        email.Html.Should().Contain("Maya Brooks")
            .And.Contain("Morning, 8 am to 12 pm")
            .And.Contain("House, owner")
            .And.Contain("href=\"tel:5125550147\"")
            .And.Contain("href=\"mailto:maya.brooks@example.com\"")
            .And.Contain("Two big oaks shade the south side.<br>Call after 9.")
            .And.Contain("Replying to this email writes to Maya, not to the office.")
            .And.Contain("Lead #128, received 13 Nov 2026, 20:58 UTC");
        email.Text.Should().Contain("NEW LEAD #128: MAYA BROOKS")
            .And.Contain("Call:          Morning, 8 am to 12 pm")
            .And.Contain("Phone:         (512) 555-0147");
    }

    [Fact]
    public void NewLeadAlert_ValuesTheFormNoLongerOffers_AreShownAsStored()
    {
        var email = LeadEmails.NewLeadAlert(Maya(l =>
        {
            l.BestTimeToContact = "Weekends";
            l.MonthlyBillRange = "About 150";
        }));

        email.Html.Should().Contain("Weekends").And.Contain("About 150");
    }

    #endregion

    #region Values from the form

    [Fact]
    public void BothEmails_MarkupInTheForm_StaysText()
    {
        var lead = Maya(l =>
        {
            l.FirstName = Link;
            l.LastName = "<b>Smith</b>";
            l.Address.Street = "1 Main St <script>alert(1)</script>";
            l.Address.City = "Austin\"><img src=x>";
            l.MonthlyBillRange = "<i>a lot</i>";
            l.Notes = "<img src=x onerror=alert(1)> Unlock it " + Link;
        });

        foreach (var email in new[] { LeadEmails.Welcome(lead, SiteUrl), LeadEmails.NewLeadAlert(lead) })
        {
            email.Html.Should().NotContain("evil.example\">")
                .And.NotContain("<img src=x")
                .And.NotContain("<script>")
                .And.NotContain("<b>Smith")
                .And.NotContain("<i>a lot")
                .And.Contain("&lt;a href=&quot;https://evil.example&quot;&gt;Verify your account&lt;/a&gt;");
            // The email's own image and links only: the wordmark; the six steps, or the phone and email
            CountOf(email.Html, "<img ").Should().Be(1);
        }
        CountOf(LeadEmails.Welcome(lead, SiteUrl).Html, "<a ").Should().Be(1);
        CountOf(LeadEmails.NewLeadAlert(lead).Html, "<a ").Should().Be(2);
    }

    private static int CountOf(string text, string part) => text.Split(part).Length - 1;

    #endregion

    #region What the email service sends

    private static (EmailService Service, CapturingSender Sender) Service()
    {
        var sender = new CapturingSender();
        var options = Options.Create(new EmailOptions
        {
            FromName = "Solaris", FromAddress = "office@example.com", SiteUrl = SiteUrl
        });
        return (new EmailService(sender, options), sender);
    }

    [Fact]
    public async Task EmailService_Welcome_GoesToThePersonWithBothPartsAndTheWordmark()
    {
        var (service, sender) = Service();

        await service.SendLeadWelcomeAsync(Maya());

        var message = sender.Sent.Single();
        message.NotificationKey.Should().Be("LeadWelcome:128");
        (message.ToAddress, message.ToName).Should().Be(("maya.brooks@example.com", "Maya Brooks"));
        message.Subject.Should().Be("We have your quote request");
        message.TextBody.Should().StartWith("WE HAVE YOUR REQUEST");
        message.ReplyTo.Should().BeNull();
        var image = message.InlineImages.Should().ContainSingle().Subject;
        (image.ContentId, image.MediaType).Should().Be((Wordmark.ContentId, "image/png"));
        image.Content.Take(4).Should().Equal(0x89, 0x50, 0x4e, 0x47);   // a PNG
    }

    [Fact]
    public async Task EmailService_Alert_GoesToTheOfficeAndRepliesGoToThePerson()
    {
        var (service, sender) = Service();

        await service.SendNewLeadAlertAsync(Maya());

        var message = sender.Sent.Single();
        message.NotificationKey.Should().Be("SalesNewLeadAlert:128");
        (message.ToAddress, message.ToName).Should().Be(("office@example.com", "Solaris"));
        message.ReplyTo.Should().Be(new EmailAddress("maya.brooks@example.com", "Maya Brooks"));
        message.InlineImages.Should().ContainSingle();
    }

    private sealed class CapturingSender : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    #endregion
}
