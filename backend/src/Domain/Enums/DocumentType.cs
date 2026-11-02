namespace Domain.Enums;

public enum DocumentType
{
    // Customer documents
    IdVerification,
    PropertyOwnership,
    UtilityBill,
    CustomerContract,
        
    // Installation documents
    BuildingPermit,
    ElectricalPermit,
    InstallationContract,
    TechnicalSpecification,
    InstallationPhoto,
    InspectionCertificate,
    WarrantyDocument,
        
    // Ticket documents
    ProblemPhoto,
    DiagnosticReport,
    ResolutionPhoto,
        
    // Generic
    Other
}