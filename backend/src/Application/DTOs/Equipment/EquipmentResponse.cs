namespace Application.DTOs.Equipment;

public class EquipmentResponse
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public decimal Cost { get; set; }
    public string? Specifications { get; set; }
    public int? InstallationId { get; set; }
    public string? InstallationName { get; set; }
    public bool IsUnderWarranty { get; set; }
}