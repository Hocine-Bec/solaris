using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Settings;

/// <summary>The <c>Jwt</c> section: how the API signs and validates staff session tokens.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// The HMAC-SHA256 signing key. A secret. At least 32 characters, because HMAC-SHA256 needs a key of at least
    /// 256 bits and shorter keys are rejected when tokens are signed.
    /// </summary>
    [Required, MinLength(32)]
    public string Secret { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>How long a session lasts, in hours: a working day (ADR 7). There are no refresh tokens in v1.</summary>
    [Range(1, 24)]
    public int LifetimeHours { get; set; } = 8;
}
