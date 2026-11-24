namespace Application.DTOs.Telemetry;

/// <summary>
/// What a batch did (FR-T2): how many readings were stored, how many were slots the installation already had, and
/// every reading that was refused, with the reason. Sending the same batch again stores nothing and refuses nothing:
/// its readings all count as duplicates.
/// </summary>
public sealed record UploadReadingsResponse(int Accepted, int Duplicates, IReadOnlyList<RejectedReadingResponse> Rejected);

/// <summary>A reading the rules refused (BR-T1), by the slot it named, so a device can tell which of its readings it was.</summary>
public sealed record RejectedReadingResponse(DateTime SlotStart, string Reason);
