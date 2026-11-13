namespace Domain.Enums;

/// <summary>A staff account's role. Customers have no account.</summary>
public enum UserRole
{
    /// <summary>The office: leads, customers, installations, production, notifications and accounts.</summary>
    Admin,

    /// <summary>Surveyors, installers and inspectors: the installations they are assigned to.</summary>
    Technician
}
