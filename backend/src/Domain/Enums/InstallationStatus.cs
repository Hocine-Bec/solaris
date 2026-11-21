namespace Domain.Enums;

/// <summary>
/// An installation's stage, from the site visit to a working system (BR-I1), or how it ended (BR-I2). A conversion
/// starts it at Survey.
/// </summary>
public enum InstallationStatus
{
    Survey,
    Design,
    Permits,
    Installation,
    Inspection,
    Active,
    Cancelled,
    Deactivated
}
