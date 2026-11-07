using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Infrastructure.Settings;

/// <summary>The <c>Email</c> section: the provider, the sender, the SMTP server and the Gmail OAuth client.</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Who sends the emails: Gmail, or nobody (Recording). <see cref="EmailOptionsValidator"/> keeps Recording out of Production.</summary>
    public EmailProvider Provider { get; set; } = EmailProvider.Gmail;

    /// <summary>Recording only: how long each email takes to "send", in milliseconds.</summary>
    [Range(0, 60_000)]
    public int SimulatedLatencyMs { get; set; } = 2000;

    /// <summary>The sender name shown on emails.</summary>
    [Required]
    public string FromName { get; set; } = string.Empty;

    /// <summary>The sender address; sales notifications are sent to it too.</summary>
    [Required, EmailAddress]
    public string FromAddress { get; set; } = string.Empty;

    [Required, ValidateObjectMembers]
    public SmtpOptions Smtp { get; set; } = new();

    /// <summary>Required only when <see cref="Provider"/> is Gmail (<see cref="EmailOptionsValidator"/>).</summary>
    public GmailOptions Gmail { get; set; } = new();
}

public enum EmailProvider
{
    /// <summary>Gmail's SMTP server, with an OAuth2 token: real emails.</summary>
    Gmail,

    /// <summary>No email is sent; each one takes <see cref="EmailOptions.SimulatedLatencyMs"/>.</summary>
    Recording
}

public sealed class SmtpOptions
{
    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; }
}

/// <summary>The Gmail OAuth client the SMTP login uses. All three values are secrets.</summary>
public sealed class GmailOptions
{
    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ClientSecret { get; set; } = string.Empty;

    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
