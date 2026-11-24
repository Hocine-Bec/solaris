using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// The energy one installation produced in one 15-minute slot (BR-T1). Built naive on purpose: a surrogate key and
/// EF Core's defaults, so there is nothing in the database to stop the same slot being stored twice. P4a measures
/// that; the fixes branch gives the table its real key (the installation and the slot) and a set-based insert.
/// </summary>
public class EnergyReading
{
    public long Id { get; set; }

    public int InstallationId { get; set; }
    public Installation Installation { get; set; } = null!;

    /// <summary>The start of the slot, in UTC: always :00, :15, :30 or :45.</summary>
    public DateTime RecordedAt { get; set; }

    public decimal EnergyKwh { get; set; }

    /// <summary>How long one slot is: a reading is the energy produced over it.</summary>
    public static readonly TimeSpan Slot = TimeSpan.FromMinutes(15);

    /// <summary>A reading older than this is refused: a device backfills at most this far (BR-T1, FR-T3).</summary>
    public static readonly TimeSpan OldestAccepted = TimeSpan.FromDays(30);

    /// <summary>
    /// More than a system of this size can physically make in one slot, with a tenth over for a very good day and for
    /// rounding: anything above it is a broken or lying device, not production (BR-T1).
    /// </summary>
    public static decimal MaximumKwh(decimal systemSizeKw) => systemSizeKw * 0.25m * 1.1m;

    /// <summary>
    /// Why this reading can't be stored (BR-T1), or null when it can: the slot must start on the quarter hour, be in
    /// the past, be no older than 30 days, and hold an amount the system could have made. Being a slot the
    /// installation already has is not refused here: that is a duplicate, which FR-T2 counts rather than rejects.
    /// </summary>
    public static Refusal? WhyNotAccepted(DateTime slotStart, decimal energyKwh, decimal? systemSizeKw, DateTime now)
    {
        if (slotStart.Ticks % Slot.Ticks != 0)
            return new Refusal("reading.not_a_slot", "A slot starts at :00, :15, :30 or :45, exactly on the minute.");
        if (slotStart > now)
            return new Refusal("reading.in_the_future", "This slot hasn't happened yet.");
        if (slotStart < now - OldestAccepted)
            return new Refusal("reading.too_old", $"Readings are accepted for the last {OldestAccepted.Days} days.");
        if (energyKwh < 0)
            return new Refusal("reading.negative", "A system can't produce less than nothing.");
        // An Active installation always has its design recorded: BR-I1 won't let it leave Design without one.
        if (systemSizeKw is { } size && energyKwh > MaximumKwh(size))
            return new Refusal("reading.above_the_maximum",
                $"More than a {size:0.##} kW system can produce in 15 minutes ({MaximumKwh(size):0.###} kWh).");
        return null;
    }
}
