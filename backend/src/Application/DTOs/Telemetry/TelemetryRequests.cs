namespace Application.DTOs.Telemetry;

/// <summary>A device's batch of readings for its own installation (FR-T1): at most 500 slots.</summary>
public class UploadReadingsRequest
{
    public const int MaxReadings = 500;

    public List<ReadingRequest> Readings { get; set; } = [];
}

/// <summary>
/// One 15-minute slot: when it started (UTC) and the energy produced in it, in watt-hours, as the devices report it.
/// </summary>
public class ReadingRequest
{
    public DateTime SlotStart { get; set; }
    public decimal EnergyWh { get; set; }
}
