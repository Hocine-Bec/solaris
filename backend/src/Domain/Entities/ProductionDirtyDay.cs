namespace Domain.Entities;

/// <summary>
/// A local day whose rollup is out of date (BR-P3): written with the readings that changed it, in the same
/// statement, and cleared by the job that recalculates the day. A day already marked stays marked once: the pair is
/// the key, so a second batch for the same day adds nothing.
/// </summary>
public class ProductionDirtyDay
{
    public int InstallationId { get; set; }
    public Installation Installation { get; set; } = null!;

    /// <summary>The day as the property lived it, in the installation's own time zone (BR-P4).</summary>
    public DateOnly Day { get; set; }
}
