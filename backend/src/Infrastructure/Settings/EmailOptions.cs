using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Infrastructure.Settings;

/// <summary>The <c>Email</c> section: the sender, the SMTP server and the Gmail OAuth client.</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>The sender name shown on emails.</summary>
    [Required]
    public string FromName { get; set; } = string.Empty;

    /// <summary>The sender address; sales notifications are sent to it too.</summary>
    [Required, EmailAddress]
    public string FromAddress { get; set; } = string.Empty;

    [Required, ValidateObjectMembers]
    public SmtpOptions Smtp { get; set; } = new();

    [Required, ValidateObjectMembers]
    public GmailOptions Gmail { get; set; } = new();
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
