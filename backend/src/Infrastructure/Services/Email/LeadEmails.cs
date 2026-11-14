using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Services.Email;

/// <summary>A rendered email: its subject, the preview line inboxes show after it, and its HTML and plain-text parts.</summary>
public sealed record RenderedEmail(string Subject, string Preheader, string Html, string Text);

/// <summary>
/// The two emails a new lead sends, from the approved design: the welcome email to the person and the office's alert.
/// Tables and inline styles, as email apps need, in the brand's colours as solid values. Every value that came from
/// the form is HTML-encoded before it goes into the HTML. The wordmark is an image sent inside the email
/// (<see cref="Wordmark"/>). Both need the lead's address.
/// </summary>
public static partial class LeadEmails
{
    // The brand's colours as solid values (email apps handle transparency poorly): the text roles over Limestone
    private const string Night = "#040405", Limestone = "#f4f2ed", Raised = "#f9f6ef", Ink = "#0b0b0c",
        Muted = "#484746", Faint = "#646362", Line = "#cfcdc9", Sun = "#ffc21f", SunDeep = "#8a5a00",
        BandText = "#a6a6a5", Info = "#2b5ea8", InfoBg = "#e6e8e9", InfoLine = "#a5b8d2";

    // Email apps don't load the brand's fonts: the closest common ones
    private const string Sans = "Arial,Helvetica,sans-serif";
    private const string Mono = "ui-monospace,Menlo,Consolas,'Courier New',monospace";

    private static readonly string[] Steps = ["Survey", "Design", "Permits", "Installation", "Inspection", "Switch-on"];

    /// <summary>To the person: what happens next, the six steps, and what they sent.</summary>
    public static RenderedEmail Welcome(Lead lead, string siteUrl)
    {
        var call = CallTime(lead.BestTimeToContact);
        var stepsLink = $"{siteUrl.TrimEnd('/')}/how-it-works";
        var type = $"{TypeLabel(lead.PropertyType)}, {(lead.IsPropertyOwner ? "you own it" : "you rent or lease it")}";
        var bill = BillLabel(lead.MonthlyBillRange);
        var hasNote = !string.IsNullOrWhiteSpace(lead.Notes);
        const string subject = "We have your quote request";
        var preheader = $"We'll call you {call} about {lead.Address.Street}.";

        var details = new List<(string Label, string Html)>
        {
            ("Property", E(AddressLine(lead.Address))),
            ("Type", E(type)),
            ("Monthly bill", E(bill)),
            ("We call", E($"{lead.PhoneNumber}, {call}")),
        };
        if (hasNote)
            details.Add(("Your note", Lines(lead.Notes!)));

        var steps = string.Join("&nbsp;&nbsp; ", Steps.Select((step, i) => i == 0
            ? $"<span style=\"color:{Ink};border-bottom:2px solid {Sun};white-space:nowrap;\">01&nbsp;{step}</span>"
            : $"<span style=\"white-space:nowrap;\">{i + 1:00}&nbsp;{step}</span>"));

        var html = Head(subject, preheader) + Band("Quote request") + $$"""
            <tr><td class="px" style="padding:40px 32px 0;">
              {{Label("Request received", SunDeep)}}
              <h1 class="h1" style="margin:0 0 16px;font:700 32px/1.05 {{Sans}};letter-spacing:-0.3px;text-transform:uppercase;color:{{Ink}};">We have your request<span style="color:{{Sun}};">.</span></h1>
              <p style="margin:0;font:400 16px/1.6 {{Sans}};color:{{Muted}};">Thanks for asking, {{E(lead.FirstName)}}. Here's what happens next, and what you sent us.</p>
            </td></tr>
            <tr><td class="px" style="padding:32px 32px 0;">
              {{Label("What happens next")}}
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">
                <tr><td width="44" valign="top" style="padding:14px 0;border-top:1px solid {{Line}};font:500 13px/1.6 {{Mono}};color:{{SunDeep}};">01</td><td style="padding:14px 0;border-top:1px solid {{Line}};font:400 16px/1.55 {{Sans}};color:{{Ink}};">We call you on {{E(lead.PhoneNumber)}} {{E(call)}}.</td></tr>
                <tr><td width="44" valign="top" style="padding:14px 0;border-top:1px solid {{Line}};border-bottom:1px solid {{Line}};font:500 13px/1.6 {{Mono}};color:{{SunDeep}};">02</td><td style="padding:14px 0;border-top:1px solid {{Line}};border-bottom:1px solid {{Line}};font:400 16px/1.55 {{Sans}};color:{{Ink}};">If you want to go ahead, we book a site survey: the first of six steps.</td></tr>
              </table>
            </td></tr>
            <tr><td class="px" style="padding:24px 32px 0;">
              <p style="margin:0 0 20px;font:400 11px/2.2 {{Mono}};letter-spacing:1.5px;text-transform:uppercase;color:{{Faint}};">{{steps}}</p>
              <table role="presentation" cellpadding="0" cellspacing="0" border="0"><tr><td style="border-radius:12px;background:{{Sun}};"><a href="{{E(stepsLink)}}" style="display:inline-block;padding:14px 22px;font:500 12px/1 {{Mono}};letter-spacing:2px;text-transform:uppercase;color:{{Night}};text-decoration:none;border-radius:12px;">See the six steps</a></td></tr></table>
            </td></tr>
            <tr><td class="px" style="padding:40px 32px 0;">
              {{Label("Your request")}}
              {{Rows(details)}}
              <p style="margin:14px 0 0;font:400 14px/1.6 {{Sans}};color:{{Muted}};">Something wrong? Reply to this email and we'll correct it.</p>
            </td></tr>
            <tr><td class="px" style="padding:28px 32px 0;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:{{InfoBg}};border:1px solid {{InfoLine}};border-radius:12px;"><tr><td style="padding:14px 16px;font:400 14px/1.55 {{Sans}};color:{{Ink}};"><b style="color:{{Info}};">A demonstration.</b> Solaris Energy is fictional: nobody will call you. This email shows what a real customer would receive.</td></tr></table>
            </td></tr>
            """ + Foot("Solaris Energy. You're receiving this email because this address was used to ask for a quote on our website. If it wasn't you, you can ignore it.");

        var text = $"""
            WE HAVE YOUR REQUEST

            Thanks for asking, {lead.FirstName}. Here's what happens next, and what you sent us.

            WHAT HAPPENS NEXT
            01  We call you on {lead.PhoneNumber} {call}.
            02  If you want to go ahead, we book a site survey: the first of six steps.

            The six steps: {string.Join(", ", Steps)}.
            See what happens at each one: {stepsLink}

            YOUR REQUEST
            Property:      {AddressLine(lead.Address)}
            Type:          {type}
            Monthly bill:  {bill}
            We call:       {lead.PhoneNumber}, {call}{(hasNote ? $"\nYour note:     {Indent(lead.Notes!, 15)}" : "")}

            Something wrong? Reply to this email and we'll correct it.

            A demonstration: Solaris Energy is fictional, so nobody will call you. This email shows what a real customer would receive.

            --
            Solaris Energy. You're receiving this email because this address was used to ask for a quote on our website. If it wasn't you, you can ignore it.
            """;
        return new RenderedEmail(subject, preheader, html, text);
    }

    /// <summary>To the office: who asked, where, when to call and how to reach them. Replies go to the person.</summary>
    public static RenderedEmail NewLeadAlert(Lead lead)
    {
        var name = $"{lead.FirstName} {lead.LastName}";
        var address = lead.Address;
        var call = OfficeCallTime(lead.BestTimeToContact);
        var property = $"{TypeLabel(lead.PropertyType)}, {(lead.IsPropertyOwner ? "owner" : "rents or leases")}";
        var bill = BillLabel(lead.MonthlyBillRange);
        var hasNote = !string.IsNullOrWhiteSpace(lead.Notes);
        var received = $"{lead.CreatedAt.ToString("d MMM yyyy, HH:mm", CultureInfo.InvariantCulture)} UTC";
        var subject = $"New lead: {name}, {address.City}, {address.State}";
        var preheader = $"{property}, bill {bill.ToLowerInvariant()}. Call {CallTime(lead.BestTimeToContact)}.";

        var contact = Rows([
            ("Phone", $"<a href=\"tel:{E(PhoneDigits().Replace(lead.PhoneNumber, ""))}\" style=\"color:{SunDeep};\">{E(lead.PhoneNumber)}</a>"),
            ("Email", $"<a href=\"mailto:{E(lead.Email)}\" style=\"color:{SunDeep};\">{E(lead.Email)}</a>"),
        ]);
        var note = hasNote ? $$"""
            <tr><td class="px" style="padding:28px 32px 0;">
              {{Label("Their note")}}
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0"><tr><td style="padding:2px 0 2px 16px;border-left:3px solid {{Sun}};font:400 15px/1.6 {{Sans}};color:{{Ink}};word-break:break-word;">{{Lines(lead.Notes!)}}</td></tr></table>
            </td></tr>
            """ : "";

        var html = Head(subject, preheader) + Band($"New lead · #{lead.Id}") + $$"""
            <tr><td class="px" style="padding:40px 32px 0;">
              {{Label("From the quote form", SunDeep)}}
              <h1 class="h1" style="margin:0 0 10px;font:700 32px/1.05 {{Sans}};letter-spacing:-0.3px;text-transform:uppercase;color:{{Ink}};word-break:break-word;">{{E(name)}}<span style="color:{{Sun}};">.</span></h1>
              <p style="margin:0;font:400 16px/1.6 {{Sans}};color:{{Muted}};">{{E(AddressLine(address))}}</p>
            </td></tr>
            <tr><td class="px" style="padding:28px 32px 0;">
              <table role="presentation" class="stack" width="100%" cellpadding="0" cellspacing="0" border="0"><tr>
                {{Fact("Call", E(call))}}
                {{Fact("Property", E(property))}}
                {{Fact("Monthly bill", E(bill))}}
              </tr></table>
            </td></tr>
            <tr><td class="px" style="padding:28px 32px 0;">
              {{Label("Contact")}}
              {{contact}}
            </td></tr>{{note}}
            <tr><td class="px" style="padding:28px 32px 0;">
              <p style="margin:0;font:400 14px/1.6 {{Sans}};color:{{Muted}};">Replying to this email writes to {{E(lead.FirstName)}}, not to the office.</p>
            </td></tr>
            """ + Foot($"Lead #{lead.Id}, received {received}, from the quote form on the website.");

        var text = $"""
            NEW LEAD #{lead.Id}: {name.ToUpperInvariant()}
            {AddressLine(address)}

            Call:          {call}
            Property:      {property}
            Monthly bill:  {bill}

            Phone:         {lead.PhoneNumber}
            Email:         {lead.Email}{(hasNote ? $"\n\nTHEIR NOTE\n{lead.Notes}" : "")}

            Replying to this email writes to {lead.FirstName}, not to the office.

            --
            Lead #{lead.Id}, received {received}, from the quote form on the website.
            """;
        return new RenderedEmail(subject, preheader, html, text);
    }

    // ---- The values, as the site words them ----

    private static string TypeLabel(PropertyType type) => type switch
    {
        PropertyType.House => "House",
        PropertyType.Apartment => "Apartment",
        PropertyType.Commercial => "Business premises",
        _ => type.ToString()
    };

    private static string BillLabel(string range) => range switch
    {
        "under100" => "Under $100",
        "100-200" => "$100 to $200",
        "200-300" => "$200 to $300",
        "300plus" => "Over $300",
        _ => range
    };

    private static (string When, string Hours)? Hours(string time) => time switch
    {
        "Morning" => ("morning", "8 am to 12 pm"),
        "Afternoon" => ("afternoon", "12 to 5 pm"),
        "Evening" => ("evening", "5 to 8 pm"),
        _ => null
    };

    /// <summary>"in the morning (8 am to 12 pm)"</summary>
    private static string CallTime(string time) =>
        Hours(time) is { } h ? $"in the {h.When} ({h.Hours})" : $"at the time you chose ({time})";

    /// <summary>"Morning, 8 am to 12 pm"</summary>
    private static string OfficeCallTime(string time) => Hours(time) is { } h ? $"{time}, {h.Hours}" : time;

    private static string AddressLine(Address a) => $"{a.Street}, {a.City}, {a.State} {a.ZipCode}";

    // ---- HTML ----

    /// <summary>HTML-encodes a value from the form.</summary>
    private static string E(string value) => WebUtility.HtmlEncode(value);

    /// <summary>A multi-line value, encoded, its line breaks kept.</summary>
    private static string Lines(string value) => LineBreaks().Replace(E(value.Trim()), "<br>");

    private static string Indent(string value, int by) => LineBreaks().Replace(value.Trim(), "\n" + new string(' ', by));

    private static string Label(string text, string color = Faint) =>
        $"<p style=\"margin:0 0 12px;font:400 11px/1.4 {Mono};letter-spacing:2px;text-transform:uppercase;color:{color};\">{text}</p>";

    private static string Fact(string label, string valueHtml) =>
        $"<td class=\"k\" valign=\"top\" style=\"padding:14px 12px 14px 0;border-top:1px solid {Line};\"><p style=\"margin:0 0 4px;font:400 11px/1.4 {Mono};letter-spacing:1.5px;text-transform:uppercase;color:{Faint};\">{label}</p><p style=\"margin:0;font:600 15px/1.45 {Sans};color:{Ink};\">{valueHtml}</p></td>";

    /// <summary>A card of labels and values; on a phone each label sits above its value.</summary>
    private static string Rows(IReadOnlyList<(string Label, string Html)> rows) =>
        $"<table role=\"presentation\" class=\"stack\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"background:{Raised};border:1px solid {Line};border-radius:16px;\">"
        + string.Concat(rows.Select((row, i) =>
        {
            var top = i > 0 ? $"border-top:1px solid {Line};" : "";
            return $"<tr><td class=\"k\" width=\"132\" valign=\"top\" style=\"padding:13px 0 13px 18px;{top}font:400 11px/1.6 {Mono};letter-spacing:1.5px;text-transform:uppercase;color:{Faint};\">{row.Label}</td>"
                + $"<td class=\"v\" valign=\"top\" style=\"padding:12px 18px;{top}font:400 15px/1.55 {Sans};color:{Ink};word-break:break-word;\">{row.Html}</td></tr>";
        }))
        + "</table>";

    private static string Head(string title, string preheader) => $$"""
        <!doctype html>
        <html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><meta name="color-scheme" content="light"><meta name="supported-color-schemes" content="light"><title>{{E(title)}}</title>
        <style>
          body { margin: 0; padding: 0; -webkit-text-size-adjust: 100%; }
          a { color: {{SunDeep}}; }
          @media (max-width: 620px) {
            .px { padding-left: 20px !important; padding-right: 20px !important; }
            .band { padding-left: 12px !important; }
            .band-label { letter-spacing: 1px !important; }
            .h1 { font-size: 26px !important; }
            .stack, .stack tbody, .stack tr, .stack td { display: block !important; width: 100% !important; box-sizing: border-box; }
            .stack td.k { padding-bottom: 2px !important; border-top: 0 !important; }
            .stack td.v { padding-top: 0 !important; border-top: 0 !important; }
            .stack tr + tr td.k { border-top: 1px solid {{Line}} !important; padding-top: 12px !important; }
          }
        </style></head>
        <body style="margin:0;padding:0;background:{{Limestone}};">
        <div style="display:none;max-height:0;overflow:hidden;opacity:0;mso-hide:all;">{{E(preheader)}}&#8199;&#65279;&#847;&#8199;&#65279;&#847;&#8199;&#65279;&#847;</div>
        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:{{Limestone}};"><tr><td align="center">
        <table role="presentation" width="600" cellpadding="0" cellspacing="0" border="0" style="width:100%;max-width:600px;">

        """;

    private static string Band(string right) => $$"""
        <tr><td class="px band" style="background:{{Night}};padding:14px 32px 14px 24px;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0"><tr>
            <td valign="middle"><img src="cid:{{Wordmark.ContentId}}" width="168" height="35" alt="Solaris" style="display:block;border:0;"></td>
            <td class="band-label" valign="middle" align="right" style="font:400 11px/1.4 {{Mono}};letter-spacing:2px;text-transform:uppercase;white-space:nowrap;color:{{BandText}};">{{E(right)}}</td>
          </tr></table></td></tr>

        """;

    private static string Foot(string text) => $$"""

        <tr><td class="px" style="padding:36px 32px 44px;"><p style="margin:0;padding-top:20px;border-top:1px solid {{Line}};font:400 12px/1.6 {{Sans}};color:{{Faint}};">{{E(text)}}</p></td></tr>
        </table></td></tr></table></body></html>
        """;

    [GeneratedRegex(@"[^\d+]")]
    private static partial Regex PhoneDigits();

    [GeneratedRegex(@"\r?\n")]
    private static partial Regex LineBreaks();
}
