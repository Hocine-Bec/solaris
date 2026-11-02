using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Generic equipment inventory (panels, inverters, etc.)
/// </summary>
public class Equipment
{
    public int Id { get; set; }
    public EquipmentType Type { get; set; }
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public EquipmentStatus Status { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public decimal Cost { get; set; }
        
    // Equipment-specific specifications (stored as JSON or separate fields)
    public string? Specifications { get; set; } // JSON: {wattage: 400, efficiency: 22.1}
        
    // Assignment
    public int? InstallationId { get; set; }
    public Installation? Installation { get; set; }
}