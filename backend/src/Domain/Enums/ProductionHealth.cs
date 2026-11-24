namespace Domain.Enums;

/// <summary>How a day went for one installation (BR-P2), worst last.</summary>
public enum ProductionHealth
{
    Excellent,
    Good,
    Warning,
    Critical,

    /// <summary>Not one reading for that day: the system said nothing at all.</summary>
    Offline
}
