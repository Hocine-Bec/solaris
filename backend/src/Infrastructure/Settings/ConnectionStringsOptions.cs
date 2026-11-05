using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Settings;

/// <summary>The <c>ConnectionStrings</c> section; <c>ConnectionStrings__Default</c> as an environment variable.</summary>
public sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    /// <summary>The PostgreSQL database of the API and of Hangfire. A secret: it holds a password.</summary>
    [Required]
    public string Default { get; set; } = string.Empty;
}
