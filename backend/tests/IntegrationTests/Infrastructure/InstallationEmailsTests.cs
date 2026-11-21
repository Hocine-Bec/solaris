using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Services.Email;

namespace IntegrationTests.Infrastructure;

// The switch-on email (FR-I7), rendered from an installation, with no database.
public class InstallationEmailsTests
{
    private static Installation PecanSt(Action<Installation>? change = null)
    {
        var installation = new Installation
        {
            Id = 35, Status = InstallationStatus.Active, TimeZone = "America/Chicago", SystemSizeKw = 8.4m, PanelCount = 21,
            // 02:30 UTC on 22 Nov is still 21 Nov in Texas
            ActivatedAt = new DateTime(2026, 11, 22, 2, 30, 0, DateTimeKind.Utc),
            Customer = new Customer { FirstName = "Grace", LastName = "Kim", Email = "grace.kim@example.com" },
            Address = new Address { Street = "61 Pecan St", City = "Round Rock", State = "TX", ZipCode = "78664" }
        };
        change?.Invoke(installation);
        return installation;
    }

    [Fact]
    public void SwitchedOn_SaysTheSystemIsOn_WithTheDayInTheInstallationsTimeZone()
    {
        var email = InstallationEmails.SwitchedOn(PecanSt());

        email.Subject.Should().Be("Your solar system is on");
        email.Text.Should().Contain("YOUR SYSTEM IS ON, GRACE")
            .And.Contain("Property:      61 Pecan St, Round Rock, TX 78664")
            .And.Contain("System:        8.4 kW, 21 panels")
            .And.Contain("Switched on:   21 Nov 2026")
            .And.Contain("A demonstration");
        email.Html.Should().Contain("21 Nov 2026").And.Contain("8.4 kW, 21 panels");
    }

    [Fact]
    public void SwitchedOn_EncodesWhatItRepeats()
    {
        var email = InstallationEmails.SwitchedOn(PecanSt(i => i.Customer.FirstName = "<b>Grace</b>"));

        email.Html.Should().Contain("&lt;b&gt;Grace&lt;/b&gt;").And.NotContain("<b>Grace</b>");
    }

    [Fact]
    public void SwitchedOn_NotSwitchedOn_Throws()
    {
        var act = () => InstallationEmails.SwitchedOn(PecanSt(i => i.ActivatedAt = null));

        act.Should().Throw<InvalidOperationException>();
    }
}
