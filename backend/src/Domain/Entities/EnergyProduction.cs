namespace Domain.Entities;

/// <summary>
/// One installation's local day, added up from its readings (FR-P1, BR-P3): what it produced and how many readings
/// that came from. It holds only what the readings say. What the day should have produced (BR-P1) and how it went
/// (BR-P2) are worked out when the day is read: the first from the system's design, fixed at switch-on, and the
/// second from the fleet's day, which a late reading for any other system can move (ADR 15).
/// </summary>
public class EnergyProduction
{
    public int InstallationId { get; set; }
    public Installation Installation { get; set; } = null!;

    /// <summary>The day as the property lived it, in the installation's own time zone (BR-P4).</summary>
    public DateOnly Day { get; set; }

    /// <summary>What it produced that day, in watt-hours.</summary>
    public int ActualWh { get; set; }

    /// <summary>
    /// How many readings the day was added up from. Nought means the system said nothing that day, which is Offline
    /// (BR-P2), however the row came to be.
    /// </summary>
    public int ReadingCount { get; set; }

    /// <summary>When the day was last added up.</summary>
    public DateTime ComputedAt { get; set; }
}
