using Application.DTOs.Common;

namespace Application.DTOs.Installations;

/// <summary>A page of the installation list (FR-I1): <c>?status=Design&amp;page=1</c>; <c>status</c> may repeat, none lists them all.</summary>
public sealed class InstallationPageRequest : PageRequest
{
    public List<string> Status { get; set; } = [];
}

/// <summary>The next stage, by name (BR-I1). Cancelled and Deactivated have their own requests.</summary>
public sealed class MoveInstallationRequest
{
    public string Status { get; set; } = string.Empty;
}

/// <summary>Why it is cancelled (BR-I2): kept as the note of its history row.</summary>
public sealed class CancelInstallationRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class DeactivateInstallationRequest
{
    public string? Note { get; set; }
}

/// <summary>The design (FR-I4): the system size, the panel count, the inverter.</summary>
public sealed class InstallationDesignRequest
{
    public decimal SystemSizeKw { get; set; }
    public int PanelCount { get; set; }
    public string? InverterType { get; set; }
}

/// <summary>One of the US time zones, by its IANA name (BR-P4).</summary>
public sealed class InstallationTimeZoneRequest
{
    public string TimeZone { get; set; } = string.Empty;
}

/// <summary>A technician, in a role: Surveyor, Installer or Inspector (FR-I5).</summary>
public sealed class AssignTechnicianRequest
{
    public int TechnicianId { get; set; }
    public string Role { get; set; } = string.Empty;
}
