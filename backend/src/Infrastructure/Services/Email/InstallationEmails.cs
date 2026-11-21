using System.Globalization;
using Domain.Entities;
using static Infrastructure.Services.Email.LeadEmails;

namespace Infrastructure.Services.Email;

/// <summary>
/// The installation's email, from the approved design: the switch-on email to the customer (FR-I7), in the lead emails'
/// style and with their helpers. Its values come from the customer's lead and the Admin's design, and are HTML-encoded.
/// </summary>
public static class InstallationEmails
{
    /// <summary>
    /// "Your system is on": the property, the system, and the day it was switched on in the installation's own time
    /// zone. Needs the installation's customer and address.
    /// </summary>
    public static RenderedEmail SwitchedOn(Installation installation)
    {
        var customer = installation.Customer;
        var activated = installation.ActivatedAt
            ?? throw new InvalidOperationException($"Installation {installation.Id} hasn't been switched on.");
        var zone = TimeZoneInfo.FindSystemTimeZoneById(installation.TimeZone);
        var day = TimeZoneInfo.ConvertTimeFromUtc(activated, zone).ToString("d MMM yyyy", CultureInfo.InvariantCulture);
        var system = $"{installation.SystemSizeKw?.ToString("0.0#", CultureInfo.InvariantCulture)} kW, {installation.PanelCount} panels";
        var property = AddressLine(installation.Address);
        const string subject = "Your solar system is on";
        var preheader = $"The system at {installation.Address.Street} was switched on {day}.";
        const string reply = "Questions about it? Reply to this email; it reaches the office.";
        var intro = $"The inspection passed, and the system at {installation.Address.Street} was switched on {day}. "
                    + "From now on it reports what it produces every 15 minutes, and our office watches it: if it ever "
                    + "makes less than it should, we'll get in touch.";

        var html = Head(subject, preheader) + Band("Switched on") + $$"""
            <tr><td class="px" style="padding:40px 32px 0;">
              {{Label("Your system", SunDeep)}}
              <h1 class="h1" style="margin:0 0 16px;font:700 32px/1.05 {{Sans}};letter-spacing:-0.3px;text-transform:uppercase;color:{{Ink}};">Your system is on, {{E(customer.FirstName)}}<span style="color:{{Sun}};">.</span></h1>
              <p style="margin:0;font:400 16px/1.6 {{Sans}};color:{{Muted}};">{{E(intro)}}</p>
            </td></tr>
            <tr><td class="px" style="padding:32px 32px 0;">
              {{Rows([("Property", E(property)), ("System", E(system)), ("Switched on", E(day))])}}
              <p style="margin:14px 0 0;font:400 14px/1.6 {{Sans}};color:{{Muted}};">{{E(reply)}}</p>
            </td></tr>
            <tr><td class="px" style="padding:28px 32px 0;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:{{InfoBg}};border:1px solid {{InfoLine}};border-radius:12px;"><tr><td style="padding:14px 16px;font:400 14px/1.55 {{Sans}};color:{{Ink}};"><b style="color:{{Info}};">A demonstration.</b> Solaris Energy is fictional: no system was installed. This email shows what a real customer would receive.</td></tr></table>
            </td></tr>
            """ + Foot("Solaris Energy. You're receiving this email because your solar system was switched on.");

        var text = $"""
            YOUR SYSTEM IS ON, {customer.FirstName.ToUpperInvariant()}

            {intro}

            Property:      {property}
            System:        {system}
            Switched on:   {day}

            {reply}

            A demonstration: Solaris Energy is fictional, so no system was installed. This email shows what a real customer would receive.

            --
            Solaris Energy. You're receiving this email because your solar system was switched on.
            """;
        return new RenderedEmail(subject, preheader, html, text);
    }
}
