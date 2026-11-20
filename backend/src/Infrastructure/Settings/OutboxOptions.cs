using System.ComponentModel.DataAnnotations;
using Infrastructure.Outbox;

namespace Infrastructure.Settings;

/// <summary>The <c>Outbox</c> section: how the outbox dispatcher sends.</summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>
    /// How many of a batch's emails are sent at once, 1 to the batch's size. 5 by default: Gmail refused emails from one
    /// account at about 10 at once (docs/measurements.md §6, the realism check); a refusal is retried anyway.
    /// </summary>
    [Range(1, OutboxProcessor.BatchSize)]
    public int MaxConcurrentSends { get; set; } = 5;
}
